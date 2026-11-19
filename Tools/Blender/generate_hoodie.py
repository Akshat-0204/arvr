"""
generate_hoodie.py
Blender Python script to procedurally model and export a low-poly Hoodie FBX asset.
Can be executed inside Blender headless mode:
blender --background --python Tools/Blender/generate_hoodie.py
"""

import bpy
import os
import math

def clean_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)

def create_hoodie():
    clean_scene()
    
    # 1. Base Torso Mesh (Cylinder)
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=24,
        radius=0.26,
        depth=0.80,
        end_fill_type='NOTHING',
        location=(0, 0, 0)
    )
    hoodie = bpy.context.active_object
    hoodie.name = "Hoodie_Mesh"
    
    # Scale torso for streetwear fit
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.transform.resize(value=(1.0, 0.62, 1.0))
    bpy.ops.object.mode_set(mode='OBJECT')
    
    # 2. Add Left & Right Long Sleeves
    for side in [1.0, -1.0]:
        bpy.ops.mesh.primitive_cylinder_add(
            vertices=14,
            radius=0.09,
            depth=0.45,
            end_fill_type='NOTHING',
            location=(side * 0.36, 0.0, 0.08)
        )
        sleeve = bpy.context.active_object
        sleeve.rotation_euler = (0, side * math.radians(52), 0)
        sleeve.name = f"Sleeve_{'R' if side > 0 else 'L'}"
        
        hoodie.select_set(True)
        sleeve.select_set(True)
        bpy.context.view_layer.objects.active = hoodie
        bpy.ops.object.join()
        
    # 3. Add 3D Hood
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=16,
        ring_count=12,
        radius=0.18,
        location=(0, -0.05, 0.38)
    )
    hood = bpy.context.active_object
    hood.scale = (1.0, 1.1, 1.15)
    
    # Cut front half of sphere to form open hood
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='DESELECT')
    # Select front faces
    bpy.ops.object.mode_set(mode='OBJECT')
    
    hoodie.select_set(True)
    hood.select_set(True)
    bpy.context.view_layer.objects.active = hoodie
    bpy.ops.object.join()
    
    # 4. Add Kangaroo Pocket
    bpy.ops.mesh.primitive_cube_add(
        size=0.1,
        location=(0, 0.17, -0.15)
    )
    pocket = bpy.context.active_object
    pocket.scale = (1.8, 0.2, 1.2)
    
    hoodie.select_set(True)
    pocket.select_set(True)
    bpy.context.view_layer.objects.active = hoodie
    bpy.ops.object.join()
    
    # 5. UV Mapping & Shading
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=66.0, island_margin=0.02)
    bpy.ops.object.mode_set(mode='OBJECT')
    
    # Material
    mat = bpy.data.materials.new(name="M_Hoodie_Default")
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (0.2, 0.2, 0.22, 1.0)
        bsdf.inputs["Roughness"].default_value = 0.8
    hoodie.data.materials.append(mat)
    
    for poly in hoodie.data.polygons:
        poly.use_smooth = True
        
    # 6. Export FBX
    project_root = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
    out_dir = os.path.join(project_root, "Assets", "Models", "Generated")
    os.makedirs(out_dir, exist_ok=True)
    out_fbx = os.path.join(out_dir, "Hoodie.fbx")
    
    bpy.ops.export_scene.fbx(
        filepath=out_fbx,
        use_selection=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y'
    )
    print(f"Successfully exported Hoodie to: {out_fbx}")

if __name__ == "__main__":
    create_hoodie()
