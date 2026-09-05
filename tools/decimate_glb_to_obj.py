import argparse
import io
import json
import math
import pathlib
import struct

from PIL import Image, ImageOps


COMPONENT_FORMATS = {
    5121: ("B", 1),
    5123: ("H", 2),
    5125: ("I", 4),
    5126: ("f", 4),
}

TYPE_COUNTS = {
    "SCALAR": 1,
    "VEC2": 2,
    "VEC3": 3,
    "VEC4": 4,
}


def read_glb(path):
    with open(path, "rb") as file:
        magic, version, total_length = struct.unpack("<4sII", file.read(12))
        if magic != b"glTF" or version != 2:
            raise ValueError("Not a GLB 2.0 file")

        json_chunk = None
        bin_chunk = None
        while file.tell() < total_length:
            chunk_length, chunk_type = struct.unpack("<II", file.read(8))
            chunk = file.read(chunk_length)
            if chunk_type == 0x4E4F534A:
                json_chunk = chunk
            elif chunk_type == 0x004E4942:
                bin_chunk = chunk

    if json_chunk is None or bin_chunk is None:
        raise ValueError("GLB is missing JSON or BIN chunk")

    return json.loads(json_chunk.decode("utf-8")), bin_chunk


def accessor_iter(gltf, binary, accessor_index):
    accessor = gltf["accessors"][accessor_index]
    view = gltf["bufferViews"][accessor["bufferView"]]
    component_type = accessor["componentType"]
    fmt, component_size = COMPONENT_FORMATS[component_type]
    count = accessor["count"]
    type_count = TYPE_COUNTS[accessor["type"]]
    view_offset = view.get("byteOffset", 0)
    accessor_offset = accessor.get("byteOffset", 0)
    stride = view.get("byteStride", component_size * type_count)
    offset = view_offset + accessor_offset
    unpack = struct.Struct("<" + fmt * type_count).unpack_from

    for i in range(count):
        values = unpack(binary, offset + i * stride)
        yield values[0] if type_count == 1 else values


def normalize(v):
    length = math.sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2])
    if length <= 0.000001:
        return (0.0, 1.0, 0.0)

    return (v[0] / length, v[1] / length, v[2] / length)


def extract_image(gltf, binary, image_index, output_path):
    image = gltf["images"][image_index]
    view = gltf["bufferViews"][image["bufferView"]]
    start = view.get("byteOffset", 0)
    end = start + view["byteLength"]
    output_path.write_bytes(binary[start:end])


def texture_bytes(gltf, binary, texture_info):
    if not texture_info:
        return None

    texture_index = texture_info.get("index")
    if texture_index is None or texture_index >= len(gltf.get("textures", [])):
        return None

    image_index = gltf["textures"][texture_index]["source"]
    image = gltf["images"][image_index]
    view = gltf["bufferViews"][image["bufferView"]]
    start = view.get("byteOffset", 0)
    end = start + view["byteLength"]
    return binary[start:end]


def extract_pbr_textures(gltf, binary, material, output_obj):
    pbr = material.get("pbrMetallicRoughness", {})
    outputs = {}

    texture_specs = (
        ("baseColor", pbr.get("baseColorTexture")),
        ("normal", material.get("normalTexture")),
        ("emissive", material.get("emissiveTexture")),
    )
    for suffix, texture_info in texture_specs:
        data = texture_bytes(gltf, binary, texture_info)
        if data is None:
            continue

        path = output_obj.with_name(output_obj.stem + "_" + suffix + ".jpg")
        path.write_bytes(data)
        outputs[suffix] = path

    metallic_roughness = texture_bytes(gltf, binary, pbr.get("metallicRoughnessTexture"))
    if metallic_roughness is not None:
        with Image.open(io.BytesIO(metallic_roughness)) as source:
            red, roughness, metallic = source.convert("RGB").split()
            smoothness = ImageOps.invert(roughness)
            packed = Image.merge("RGBA", (metallic, metallic, metallic, smoothness))
            path = output_obj.with_name(output_obj.stem + "_metallicSmoothness.png")
            packed.save(path, "PNG", optimize=True)
            outputs["metallicSmoothness"] = path

    return outputs


def decimate_to_obj(source, output_obj, cells, uv_bins):
    gltf, binary = read_glb(source)
    primitive = gltf["meshes"][0]["primitives"][0]
    attributes = primitive["attributes"]

    positions = list(accessor_iter(gltf, binary, attributes["POSITION"]))
    normals = list(accessor_iter(gltf, binary, attributes.get("NORMAL")))
    uvs = list(accessor_iter(gltf, binary, attributes.get("TEXCOORD_0")))
    indices = list(accessor_iter(gltf, binary, primitive["indices"]))

    min_x = min(v[0] for v in positions)
    min_y = min(v[1] for v in positions)
    min_z = min(v[2] for v in positions)
    max_x = max(v[0] for v in positions)
    max_y = max(v[1] for v in positions)
    max_z = max(v[2] for v in positions)
    extent = max(max_x - min_x, max_y - min_y, max_z - min_z)
    cell_size = extent / max(1, cells)

    clusters = {}
    vertex_to_cluster = [0] * len(positions)

    for i, position in enumerate(positions):
        uv = uvs[i] if i < len(uvs) else (0.0, 0.0)
        key = (
            int((position[0] - min_x) / cell_size),
            int((position[1] - min_y) / cell_size),
            int((position[2] - min_z) / cell_size),
            int(uv[0] * uv_bins),
            int(uv[1] * uv_bins),
        )
        cluster = clusters.get(key)
        if cluster is None:
            cluster = [0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0]
            clusters[key] = cluster

        normal = normals[i] if i < len(normals) else (0.0, 1.0, 0.0)
        cluster[0] += position[0]
        cluster[1] += position[1]
        cluster[2] += position[2]
        cluster[3] += normal[0]
        cluster[4] += normal[1]
        cluster[5] += normal[2]
        cluster[6] += uv[0]
        cluster[7] += uv[1]
        cluster[8] += 1
        vertex_to_cluster[i] = id(cluster)

    cluster_id_to_obj_index = {}
    vertices = []
    obj_index = 1
    for cluster in clusters.values():
        cluster_id_to_obj_index[id(cluster)] = obj_index
        count = cluster[8]
        position = (cluster[0] / count, cluster[1] / count, cluster[2] / count)
        normal = normalize((cluster[3] / count, cluster[4] / count, cluster[5] / count))
        uv = (cluster[6] / count, cluster[7] / count)
        vertices.append((position, normal, uv))
        obj_index += 1

    faces = []
    seen_faces = set()
    for i in range(0, len(indices), 3):
        a = cluster_id_to_obj_index[vertex_to_cluster[indices[i]]]
        b = cluster_id_to_obj_index[vertex_to_cluster[indices[i + 1]]]
        c = cluster_id_to_obj_index[vertex_to_cluster[indices[i + 2]]]
        if a == b or b == c or c == a:
            continue

        ordered = tuple(sorted((a, b, c)))
        if ordered in seen_faces:
            continue

        seen_faces.add(ordered)
        faces.append((a, b, c))

    output_obj.parent.mkdir(parents=True, exist_ok=True)
    material_name = output_obj.stem
    mtl_path = output_obj.with_suffix(".mtl")

    material = gltf["materials"][primitive.get("material", 0)]
    texture_paths = extract_pbr_textures(gltf, binary, material, output_obj)
    texture_path = texture_paths.get("baseColor")

    with open(mtl_path, "w", encoding="utf-8", newline="\n") as file:
        file.write("newmtl " + material_name + "\n")
        file.write("Ka 1.000 1.000 1.000\n")
        file.write("Kd 1.000 1.000 1.000\n")
        file.write("Ks 0.080 0.080 0.080\n")
        file.write("Ns 40.000\n")
        file.write("d 1.000\n")
        file.write("illum 2\n")
        if texture_path is not None:
            file.write("map_Kd " + texture_path.name + "\n")

    with open(output_obj, "w", encoding="utf-8", newline="\n") as file:
        file.write("# Generated from " + source.name + "\n")
        file.write("# cells=" + str(cells) + " uv_bins=" + str(uv_bins) + "\n")
        file.write("mtllib " + mtl_path.name + "\n")
        file.write("o " + material_name + "\n")
        for position, normal, uv in vertices:
            file.write("v %.6f %.6f %.6f\n" % position)
        for position, normal, uv in vertices:
            file.write("vt %.6f %.6f\n" % uv)
        for position, normal, uv in vertices:
            file.write("vn %.6f %.6f %.6f\n" % normal)
        file.write("usemtl " + material_name + "\n")
        for a, b, c in faces:
            file.write("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n".format(a, b, c))

    print("source_vertices", len(positions))
    print("source_triangles", len(indices) // 3)
    print("output_vertices", len(vertices))
    print("output_triangles", len(faces))
    print("output_obj", output_obj)
    for texture_kind, path in texture_paths.items():
        print("output_" + texture_kind, path)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=pathlib.Path)
    parser.add_argument("output", type=pathlib.Path)
    parser.add_argument("--cells", type=int, default=42)
    parser.add_argument("--uv-bins", type=int, default=16)
    args = parser.parse_args()
    decimate_to_obj(args.source, args.output, args.cells, args.uv_bins)


if __name__ == "__main__":
    main()
