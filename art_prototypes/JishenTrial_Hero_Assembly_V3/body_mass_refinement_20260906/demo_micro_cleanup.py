import bmesh
def clean(o):
 original=o.data.copy();bm=bmesh.new();bm.from_mesh(o.data)
 faces=[f for f in bm.faces if f.calc_area()<1e-13]
 if not faces:bm.free();return False
 edges=list({e for f in faces for e in f.edges})
 bmesh.ops.dissolve_degenerate(bm,dist=.0000002,edges=edges)
 bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
 if any(not e.is_manifold for e in bm.edges) or any(f.calc_area()<1e-13 for f in bm.faces):bm.free();return False
 bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free();o.data.update();return True
