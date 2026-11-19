"""
generate_tshirt.py
Blender Python script to procedurally model and export a low-poly T-Shirt FBX asset.
Can be executed inside Blender headless mode:
blender --background --python Tools/Blender/generate_tshirt.py
"""

import bpy
import os
import math

def clean_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)

def create_tshirt():
    clean_scene()
    
    # 1. Base Torso Mesh (Cylinder)
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=24,
        radius=0.24,
        depth=0.75,
        end_fill_type='NOTHING',
        location=(0, 0, 0)
    )
    torso = bpy.context.active_object
    torso.name = "TShirt_Mesh"
    
    # Scale torso slightly on Z/X to make chest realistic (flatten front-to-back)
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.transform.resize(value=(1.0, 0.58, 1.0))
    bpy.ops.object.mode_set(mode='OBJECT')
    
    # 2. Add Left & Right Sleeves
    for side in [1.0, -1.0]:
        bpy.ops.mesh.primitive_cylinder_add(
            vertices=14,
            radius=0.08,
            depth=0.22,
            end_fill_type='NOTHING',
            location=(side * 0.28, 0.0, 0.22)
        )
        sleeve = bpy.context.active_object
        sleeve.rotation_euler = (0, side * math.radians(45), 0)
        sleeve.name = f"Sleeve_{'R' if side > 0 else 'L'}"
        
        # Join into torso
        torso.select_set(True)
        sleeve.select_set(True)
        bpy.context.view_layer.objects.active = torso
        bpy.ops.object.join()
        
    # 3. Add Material & UVs
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=66.0, island_margin=0.02)
    bpy.ops.object.mode_set(mode='OBJECT')
    
    # Create default material
    mat = bpy.data.materials.new(name="M_TShirt_Default")
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (0.85, 0.85, 0.88, 1.0)
        bsdf.inputs["Roughness"].default_value = 0.7
    torso.data.materials.append(mat)
    
    # Smooth Shading
    for poly in torso.data.polygons:
        poly.use_smooth = True
        
    # 4. Export FBX
    project_root = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
    out_dir = os.path.join(project_root, "Assets", "Models", "Generated")
    os.makedirs(out_dir, exist_ok=True)
    out_fbx = os.path.join(out_dir, "TShirt.fbx")
    
    bpy.ops.export_scene.fbx(
        filepath=out_fbx,
        use_selection=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y'
    )
    print(f"Successfully exported TShirt to: {out_fbx}")

if __name__ == "__main__":
    create_tshirt()
