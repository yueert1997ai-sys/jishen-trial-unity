import pathlib,sys,importlib
W=pathlib.Path(__file__).resolve().parent;sys.path.insert(0,str(W))
stage=int(next((a.split('=')[1] for a in sys.argv if a.startswith('stage=')),'1'))
import prepare_body
import body_common as c
import body_torso
if stage>=2:import body_limbs
if stage>=3:import body_equipment
if stage>=4:import body_finish
if stage>=5:import body_final_fit
if stage>=6:import body_depth_pass
if stage>=7:import body_side_cleanup
if stage>=8:import body_profile_fit
if stage>=9:import body_final_surface
import body_review_setup
c.save_stage(stage,{1:'Reference chest, abdomen, hip belt, swept shoulder wings. Original mechanical components remapped to reference standing pivots.',2:'Layered forearms, white thigh mechanics, tapered calves, articulated fingers and angular split-toe feet.',3:'Twin-thruster backpack with diagonal vanes, shoulder cannon and held anti-ship beam sword.',4:'Complete body reference refinement with final surface and fit corrections.'}[min(stage,4)])
