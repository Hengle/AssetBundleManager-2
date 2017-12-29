/******************************************************************************
 * Spine Runtimes Software License
 * Version 2.3
 * 
 * Copyright (c) 2013-2015, Esoteric Software
 * All rights reserved.
 * 
 * You are granted a perpetual, non-exclusive, non-sublicensable and
 * non-transferable license to use, install, execute and perform the Spine
 * Runtimes Software (the "Software") and derivative works solely for personal
 * or internal use. Without the written permission of Esoteric Software (see
 * Section 2 of the Spine Software License Agreement), you may not (a) modify,
 * translate, adapt or otherwise create derivative works, improvements of the
 * Software or develop new applications using the Software or (b) remove,
 * delete, alter or obscure any trademarks or any copyright, trademark, patent
 * or other intellectual property or proprietary rights notices on or in the
 * Software, including any copy thereof. Redistributions in binary or source
 * form must include this license and terms.
 * 
 * THIS SOFTWARE IS PROVIDED BY ESOTERIC SOFTWARE "AS IS" AND ANY EXPRESS OR
 * IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO
 * EVENT SHALL ESOTERIC SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS;
 * OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
 * OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *****************************************************************************/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Spine;

[ExecuteInEditMode, RequireComponent(typeof(CanvasRenderer))]
[AddComponentMenu("Spine/SkeletonGraphic (Unity UI Canvas)")]
public class SkeletonGraphic : Graphic
{
    #region Inspector
    [Header("Skeleton Renderer")]
    public SkeletonDataAsset skeletonDataAsset;

    [SpineSkin(dataField: "skeletonDataAsset")]
    public string initialSkinName = "default";

    [Header("Skeleton Animation")]
    [SerializeField]
    [SpineAnimation(dataField: "skeletonDataAsset")]
    private string m_AnimationName;
    public float timeScale = 1;
    public bool loop;

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        // This handles Scene View preview.
        base.OnValidate();
        this.raycastTarget = false;

        if (valid)
        {
            if (skeletonDataAsset == null)
            {
                Clear();
                m_AnimationName = "";
            }
            else if (skeletonDataAsset.GetSkeletonData(true) != skeleton.data)
            {
                Initialize();
                m_AnimationName = "";
            }
            else
            {
                if (freeze) return;

                skeleton.SetToSetupPose();
                if (state == null) Debug.Log("state was null for some reason");
                state.ClearTracks(); // Need this. Otherwise, it mixes between animations. It's hilarious.
                state.SetAnimation(0, m_AnimationName, this.loop);
                Update(0);
                skeleton.UpdateWorldTransform();

                this.material = sharedMaterials[0];
                UpdateMesh();
            }
        }
        else
        {
            if (skeletonDataAsset != null)
                Initialize();
        }
    }
#endif
    #endregion

    #region Non-Inspector API
    internal Skeleton skeleton;
    public Skeleton Skeleton
    {   // TODO: Add this to official
        get
        {
            if (skeleton == null) Initialize();
            return skeleton;
        }
    }

    internal Spine.AnimationState state;
    public Spine.AnimationState State
    {   // TODO: Add this to official
        get
        {
            if (state == null) Initialize();
            return state;
        }
    }
    #endregion

    #region Advanced Settings
    [Header("Advanced")]
    public bool freeze = false;
    public bool calculateNormals;
    public bool calculateTangents;
    public float zSpacing = 0;

    public bool renderMeshes = true, immutableTriangles;
    public bool frontFacing;
    public bool logErrors = false;
    #endregion

    #region Internals
    [System.NonSerialized]
    public bool valid;

    Mesh mesh1, mesh2;
    bool useMesh1;
    float[] tempVertices = new float[8];
    Vector3[] vertices;
    Color32[] colors;
    Vector2[] uvs;
    Material[] sharedMaterials = new Material[0];

    readonly ExposedList<Material> submeshMaterials = new ExposedList<Material>();
    readonly ExposedList<Submesh> submeshes = new ExposedList<Submesh>();

    LastState lastState = new LastState();
    #endregion

    #region SkeletonAnimation
    public delegate void SkeletonGraphicDelegate(SkeletonGraphic skeletonGraphic);
    protected event SkeletonGraphicDelegate m_UpdateLocal;
    protected event SkeletonGraphicDelegate m_UpdateWorld;
    protected event SkeletonGraphicDelegate m_UpdateComplete;
    public event SkeletonGraphicDelegate UpdateLocal { add { m_UpdateLocal += value; } remove { m_UpdateLocal -= value; } }
    public event SkeletonGraphicDelegate UpdateWorld { add { m_UpdateWorld += value; } remove { m_UpdateWorld -= value; } }
    public event SkeletonGraphicDelegate UpdateComplete { add { m_UpdateComplete += value; } remove { m_UpdateComplete -= value; } }

    public string AnimationName
    {
        get
        {
            if (state == null) return null;

            TrackEntry entry = state.GetCurrent(0);
            return entry == null ? null : entry.Animation.Name;
        }
        set
        {
            if (m_AnimationName == value) return;

            m_AnimationName = value;
            if (string.IsNullOrEmpty(value))
                state.ClearTrack(0);
            else
                state.SetAnimation(0, value, loop);
        }
    }

    public virtual void Update()
    {
        if (freeze) return;
        Update(Time.deltaTime);
    }

    public virtual void Update(float deltaTime)
    {
        if (!valid) return;

        deltaTime *= timeScale;
        skeleton.Update(deltaTime);
        state.Update(deltaTime);
        state.Apply(skeleton);

        if (m_UpdateLocal != null) m_UpdateLocal(this);

        skeleton.UpdateWorldTransform();

        if (m_UpdateWorld != null)
        {
            m_UpdateWorld(this);
            skeleton.UpdateWorldTransform();
        }

        if (m_UpdateComplete != null) m_UpdateComplete(this);
    }
    #endregion

    #region UI.Graphic
    protected override void Awake()
    {
        base.Awake();
        //Debug.Log("Awake");
        if (!valid)
        {
            Initialize();
            Rebuild(CanvasUpdate.PreRender);
        }
    }

    public override void Rebuild(CanvasUpdate update)
    {
        if (canvasRenderer.cull) return;
        //Debug.Log("Rebuild");
#if UNITY_EDITOR
        if (Application.isEditor) Skeleton.UpdateWorldTransform();
#endif

        if (update == CanvasUpdate.PreRender) UpdateMesh();
    }

    void LateUpdate()
    {
        if (freeze) return;
        UpdateMesh();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        DestroyWorkingMeshes();
    }


    #endregion

    #region SkeletonRenderer
    void DestroyWorkingMeshes()
    {
        //Debug.Log("Destroy Meshes.");
        if (mesh1 != null)
        {
            if (Application.isPlaying)
                Destroy(mesh1);
            else
                DestroyImmediate(mesh1);
        }

        if (mesh2 != null)
        {
            if (Application.isPlaying)
                Destroy(mesh2);
            else
                DestroyImmediate(mesh2);
        }

        mesh1 = null;
        mesh2 = null;
    }

    void Clear()
    {
        //Debug.Log("Clear");
        DestroyWorkingMeshes();
        canvasRenderer.Clear();

        lastState = new LastState();
        useMesh1 = false;
        vertices = null;
        colors = null;
        uvs = null;
        sharedMaterials = new Material[0];
        submeshMaterials.Clear();
        submeshes.Clear();
        // TODO: Check fix with a known repro case.
        //lastState.forceUpdateMesh1 = true;
        //lastState.forceUpdateMesh2 = true;

        skeleton = null;
        state = null; // SkeletonAnimation
        valid = false;
    }

    //protected override void Reset()
    //{
    //    //Debug.Log("Reset");
    //    base.Reset();
    //    Initialize();
    //}

    public virtual void Initialize()
    {
        //Debug.Log("Initialize");
        Clear();

        if (!skeletonDataAsset) { if (logErrors) Debug.LogError("Missing SkeletonData asset.", this); return; }
        SkeletonData skeletonData = skeletonDataAsset.GetSkeletonData(false); if (skeletonData == null) return;

        valid = true;
        mesh1 = NewMesh();
        mesh2 = NewMesh();
        vertices = new Vector3[0];

        skeleton = new Skeleton(skeletonData);
        if (!string.IsNullOrEmpty(initialSkinName) && initialSkinName != "default")
            skeleton.SetSkin(initialSkinName);

        UpdateMesh();

        // SkeletonAnimation
        state = new Spine.AnimationState(skeletonDataAsset.GetAnimationStateData());
        if (!string.IsNullOrEmpty(m_AnimationName))
        {
            state.SetAnimation(0, m_AnimationName, loop);
            Update(0);
        }
    }

    protected static Mesh NewMesh()
    {
        var mesh = new Mesh();
        mesh.name = "Skeleton Mesh";
        mesh.hideFlags = HideFlags.HideAndDontSave;
        mesh.MarkDynamic();
        return mesh;
    }

    public void UpdateMesh()
    {
        //Debug.Log("UpdateMesh");
        if (!valid) return;

        float scale = canvas.referencePixelsPerUnit;

        // Count vertices and submesh triangles.
        int vertexCount = 0;
        int submeshTriangleCount = 0, submeshFirstVertex = 0, submeshStartSlotIndex = 0;
        Material lastMaterial = null;
        ExposedList<Slot> drawOrder = skeleton.drawOrder;
        int drawOrderCount = drawOrder.Count;
        bool renderMeshes = this.renderMeshes;

        // Clear last state of attachments and submeshes
        ExposedList<int> attachmentsTriangleCountTemp = lastState.attachmentsTriangleCountTemp;
        attachmentsTriangleCountTemp.GrowIfNeeded(drawOrderCount);
        attachmentsTriangleCountTemp.Count = drawOrderCount;
        ExposedList<bool> attachmentsFlipStateTemp = lastState.attachmentsFlipStateTemp;
        attachmentsFlipStateTemp.GrowIfNeeded(drawOrderCount);
        attachmentsFlipStateTemp.Count = drawOrderCount;

        ExposedList<LastState.AddSubmeshArguments> addSubmeshArgumentsTemp = lastState.addSubmeshArgumentsTemp;
        addSubmeshArgumentsTemp.Clear(false);
        for (int i = 0; i < drawOrderCount; i++)
        {
            Slot slot = drawOrder.Items[i];
            Bone bone = slot.bone;
            Attachment attachment = slot.attachment;

            object rendererObject;
            int attachmentVertexCount, attachmentTriangleCount;
            bool worldScaleXIsPositive = bone.worldScaleX >= 0f;
            bool worldScaleYIsPositive = bone.worldScaleY >= 0f;
            bool worldScaleIsSameSigns = (worldScaleXIsPositive && worldScaleYIsPositive) ||
                (!worldScaleXIsPositive && !worldScaleYIsPositive);
            bool flip = frontFacing && ((bone.worldFlipX != bone.worldFlipY) == worldScaleIsSameSigns);
            attachmentsFlipStateTemp.Items[i] = flip;

            attachmentsTriangleCountTemp.Items[i] = -1;
            var regionAttachment = attachment as RegionAttachment;
            if (regionAttachment != null)
            {
                rendererObject = regionAttachment.RendererObject;
                attachmentVertexCount = 4;
                attachmentTriangleCount = 6;
            }
            else
            {
                if (!renderMeshes)
                    continue;
                var meshAttachment = attachment as MeshAttachment;
                if (meshAttachment != null)
                {
                    rendererObject = meshAttachment.RendererObject;
                    attachmentVertexCount = meshAttachment.vertices.Length >> 1;
                    attachmentTriangleCount = meshAttachment.triangles.Length;
                }
                else
                {
                    var skinnedMeshAttachment = attachment as SkinnedMeshAttachment;
                    if (skinnedMeshAttachment != null)
                    {
                        rendererObject = skinnedMeshAttachment.RendererObject;
                        attachmentVertexCount = skinnedMeshAttachment.uvs.Length >> 1;
                        attachmentTriangleCount = skinnedMeshAttachment.triangles.Length;
                    }
                    else
                        continue;
                }
            }

            // Populate submesh when material changes.
#if !SPINE_TK2D
            var currentMaterial = (Material)((AtlasRegion)rendererObject).page.rendererObject;
#else
			var currentMaterial = (rendererObject.GetType() == typeof(Material)) ? (Material)rendererObject : (Material)((AtlasRegion)rendererObject).page.rendererObject;
#endif
            if ((lastMaterial != null && lastMaterial.GetInstanceID() != currentMaterial.GetInstanceID()))
            {
                addSubmeshArgumentsTemp.Add(
                    new LastState.AddSubmeshArguments(lastMaterial, submeshStartSlotIndex, i, submeshTriangleCount, submeshFirstVertex, false)
                );
                submeshTriangleCount = 0;
                submeshFirstVertex = vertexCount;
                submeshStartSlotIndex = i;
            }
            lastMaterial = currentMaterial;

            submeshTriangleCount += attachmentTriangleCount;
            vertexCount += attachmentVertexCount;

            attachmentsTriangleCountTemp.Items[i] = attachmentTriangleCount;
        }
        addSubmeshArgumentsTemp.Add(
            new LastState.AddSubmeshArguments(lastMaterial, submeshStartSlotIndex, drawOrderCount, submeshTriangleCount, submeshFirstVertex, true)
        );

        bool mustUpdateMeshStructure = CheckIfMustUpdateMeshStructure(attachmentsTriangleCountTemp, attachmentsFlipStateTemp, addSubmeshArgumentsTemp);
        if (mustUpdateMeshStructure)
        {
            submeshMaterials.Clear();
            for (int i = 0, n = addSubmeshArgumentsTemp.Count; i < n; i++)
            {
                LastState.AddSubmeshArguments arguments = addSubmeshArgumentsTemp.Items[i];
                AddSubmesh(
                    arguments.material,
                    arguments.startSlot,
                    arguments.endSlot,
                    arguments.triangleCount,
                    arguments.firstVertex,
                    arguments.lastSubmesh,
                    attachmentsFlipStateTemp
                );
            }

            // Set materials.
            if (submeshMaterials.Count == sharedMaterials.Length)
                submeshMaterials.CopyTo(sharedMaterials);
            else
                sharedMaterials = submeshMaterials.ToArray();

            //meshRenderer.sharedMaterials = sharedMaterials;
            this.material = sharedMaterials[0];
            canvasRenderer.SetMaterial(sharedMaterials[0], (Texture)null);
        }

        // Ensure mesh data is the right size.
        Vector3[] vertices = this.vertices;
        bool newTriangles = vertexCount > vertices.Length;
        if (newTriangles)
        {
            // Not enough vertices, increase size.
            this.vertices = vertices = new Vector3[vertexCount];
            this.colors = new Color32[vertexCount];
            this.uvs = new Vector2[vertexCount];
            mesh1.Clear();
            mesh2.Clear();
        }
        else
        {
            // Too many vertices, zero the extra.
            Vector3 zero = Vector3.zero;
            for (int i = vertexCount, n = lastState.vertexCount; i < n; i++)
                vertices[i] = zero;
        }
        lastState.vertexCount = vertexCount;

        // Setup mesh.
        float zSpacing = this.zSpacing;
        float[] tempVertices = this.tempVertices;
        Vector2[] uvs = this.uvs;
        Color32[] colors = this.colors;
        int vertexIndex = 0;
        Color32 vertColor = new Color32();
        Color graphicColor = base.color;
        float a = skeleton.a * 255, r = skeleton.r, g = skeleton.g, b = skeleton.b;


        // Mesh bounds
        Vector3 meshBoundsMin;
        meshBoundsMin.x = float.MaxValue;
        meshBoundsMin.y = float.MaxValue;
        meshBoundsMin.z = zSpacing > 0f ? 0f : zSpacing * (drawOrderCount - 1);
        Vector3 meshBoundsMax;
        meshBoundsMax.x = float.MinValue;
        meshBoundsMax.y = float.MinValue;
        meshBoundsMax.z = zSpacing < 0f ? 0f : zSpacing * (drawOrderCount - 1);

        for (int i = 0; i < drawOrderCount; i++)
        {
            Slot slot = drawOrder.Items[i];
            Attachment attachment = slot.attachment;
            var regionAttachment = attachment as RegionAttachment;
            if (regionAttachment != null)
            {
                regionAttachment.ComputeWorldVertices(slot.bone, tempVertices);

                float z = i * zSpacing;
                vertices[vertexIndex].x = tempVertices[RegionAttachment.X1] * scale;
                vertices[vertexIndex].y = tempVertices[RegionAttachment.Y1] * scale;
                vertices[vertexIndex].z = z;
                vertices[vertexIndex + 1].x = tempVertices[RegionAttachment.X4] * scale;
                vertices[vertexIndex + 1].y = tempVertices[RegionAttachment.Y4] * scale;
                vertices[vertexIndex + 1].z = z;
                vertices[vertexIndex + 2].x = tempVertices[RegionAttachment.X2] * scale;
                vertices[vertexIndex + 2].y = tempVertices[RegionAttachment.Y2] * scale;
                vertices[vertexIndex + 2].z = z;
                vertices[vertexIndex + 3].x = tempVertices[RegionAttachment.X3] * scale;
                vertices[vertexIndex + 3].y = tempVertices[RegionAttachment.Y3] * scale;
                vertices[vertexIndex + 3].z = z;

                vertColor.a = (byte)(a * slot.a * regionAttachment.a * graphicColor.a);
                vertColor.r = (byte)(r * slot.r * regionAttachment.r * graphicColor.r * vertColor.a);
                vertColor.g = (byte)(g * slot.g * regionAttachment.g * graphicColor.g * vertColor.a);
                vertColor.b = (byte)(b * slot.b * regionAttachment.b * graphicColor.b * vertColor.a);
                if (slot.data.blendMode == BlendMode.additive) vertColor.a = 0;
                colors[vertexIndex] = vertColor;
                colors[vertexIndex + 1] = vertColor;
                colors[vertexIndex + 2] = vertColor;
                colors[vertexIndex + 3] = vertColor;

                float[] regionUVs = regionAttachment.uvs;
                uvs[vertexIndex].x = regionUVs[RegionAttachment.X1];
                uvs[vertexIndex].y = regionUVs[RegionAttachment.Y1];
                uvs[vertexIndex + 1].x = regionUVs[RegionAttachment.X4];
                uvs[vertexIndex + 1].y = regionUVs[RegionAttachment.Y4];
                uvs[vertexIndex + 2].x = regionUVs[RegionAttachment.X2];
                uvs[vertexIndex + 2].y = regionUVs[RegionAttachment.Y2];
                uvs[vertexIndex + 3].x = regionUVs[RegionAttachment.X3];
                uvs[vertexIndex + 3].y = regionUVs[RegionAttachment.Y3];


                // Calculate Bounds min/max X
                if (tempVertices[RegionAttachment.X1] < meshBoundsMin.x)
                    meshBoundsMin.x = tempVertices[RegionAttachment.X1];
                else if (tempVertices[RegionAttachment.X1] > meshBoundsMax.x)
                    meshBoundsMax.x = tempVertices[RegionAttachment.X1];
                if (tempVertices[RegionAttachment.X2] < meshBoundsMin.x)
                    meshBoundsMin.x = tempVertices[RegionAttachment.X2];
                else if (tempVertices[RegionAttachment.X2] > meshBoundsMax.x)
                    meshBoundsMax.x = tempVertices[RegionAttachment.X2];
                if (tempVertices[RegionAttachment.X3] < meshBoundsMin.x)
                    meshBoundsMin.x = tempVertices[RegionAttachment.X3];
                else if (tempVertices[RegionAttachment.X3] > meshBoundsMax.x)
                    meshBoundsMax.x = tempVertices[RegionAttachment.X3];
                if (tempVertices[RegionAttachment.X4] < meshBoundsMin.x)
                    meshBoundsMin.x = tempVertices[RegionAttachment.X4];
                else if (tempVertices[RegionAttachment.X4] > meshBoundsMax.x)
                    meshBoundsMax.x = tempVertices[RegionAttachment.X4];

                // Calculate Bounds min/max Y
                if (tempVertices[RegionAttachment.Y1] < meshBoundsMin.y)
                    meshBoundsMin.y = tempVertices[RegionAttachment.Y1];
                else if (tempVertices[RegionAttachment.Y1] > meshBoundsMax.y)
                    meshBoundsMax.y = tempVertices[RegionAttachment.Y1];
                if (tempVertices[RegionAttachment.Y2] < meshBoundsMin.y)
                    meshBoundsMin.y = tempVertices[RegionAttachment.Y2];
                else if (tempVertices[RegionAttachment.Y2] > meshBoundsMax.y)
                    meshBoundsMax.y = tempVertices[RegionAttachment.Y2];
                if (tempVertices[RegionAttachment.Y3] < meshBoundsMin.y)
                    meshBoundsMin.y = tempVertices[RegionAttachment.Y3];
                else if (tempVertices[RegionAttachment.Y3] > meshBoundsMax.y)
                    meshBoundsMax.y = tempVertices[RegionAttachment.Y3];
                if (tempVertices[RegionAttachment.Y4] < meshBoundsMin.y)
                    meshBoundsMin.y = tempVertices[RegionAttachment.Y4];
                else if (tempVertices[RegionAttachment.Y4] > meshBoundsMax.y)
                    meshBoundsMax.y = tempVertices[RegionAttachment.Y4];

                vertexIndex += 4;
            }
            else
            {
                if (!renderMeshes)
                    continue;
                var meshAttachment = attachment as MeshAttachment;
                if (meshAttachment != null)
                {
                    int meshVertexCount = meshAttachment.vertices.Length;
                    if (tempVertices.Length < meshVertexCount)
                        this.tempVertices = tempVertices = new float[meshVertexCount];
                    meshAttachment.ComputeWorldVertices(slot, tempVertices);

                    vertColor.a = (byte)(a * slot.a * meshAttachment.a * graphicColor.a);
                    vertColor.r = (byte)(r * slot.r * meshAttachment.r * graphicColor.r * vertColor.a);
                    vertColor.g = (byte)(g * slot.g * meshAttachment.g * graphicColor.g * vertColor.a);
                    vertColor.b = (byte)(b * slot.b * meshAttachment.b * graphicColor.b * vertColor.a);
                    if (slot.data.blendMode == BlendMode.additive) vertColor.a = 0;

                    float[] meshUVs = meshAttachment.uvs;
                    float z = i * zSpacing;
                    for (int ii = 0; ii < meshVertexCount; ii += 2, vertexIndex++)
                    {
                        vertices[vertexIndex].x = tempVertices[ii] * scale;
                        vertices[vertexIndex].y = tempVertices[ii + 1] * scale;
                        vertices[vertexIndex].z = z;
                        colors[vertexIndex] = vertColor;
                        uvs[vertexIndex].x = meshUVs[ii];
                        uvs[vertexIndex].y = meshUVs[ii + 1];

                        // Calculate Bounds
                        if (tempVertices[ii] < meshBoundsMin.x)
                            meshBoundsMin.x = tempVertices[ii];
                        else if (tempVertices[ii] > meshBoundsMax.x)
                            meshBoundsMax.x = tempVertices[ii];
                        if (tempVertices[ii + 1] < meshBoundsMin.y)
                            meshBoundsMin.y = tempVertices[ii + 1];
                        else if (tempVertices[ii + 1] > meshBoundsMax.y)
                            meshBoundsMax.y = tempVertices[ii + 1];

                    }
                }
                else
                {
                    var skinnedMeshAttachment = attachment as SkinnedMeshAttachment;
                    if (skinnedMeshAttachment != null)
                    {
                        int meshVertexCount = skinnedMeshAttachment.uvs.Length;
                        if (tempVertices.Length < meshVertexCount)
                            this.tempVertices = tempVertices = new float[meshVertexCount];
                        skinnedMeshAttachment.ComputeWorldVertices(slot, tempVertices);

                        vertColor.a = (byte)(a * slot.a * skinnedMeshAttachment.a * graphicColor.a);
                        vertColor.r = (byte)(r * slot.r * skinnedMeshAttachment.r * graphicColor.r * vertColor.a);
                        vertColor.g = (byte)(g * slot.g * skinnedMeshAttachment.g * graphicColor.g * vertColor.a);
                        vertColor.b = (byte)(b * slot.b * skinnedMeshAttachment.b * graphicColor.b * vertColor.a);
                        if (slot.data.blendMode == BlendMode.additive) vertColor.a = 0;

                        float[] meshUVs = skinnedMeshAttachment.uvs;
                        float z = i * zSpacing;
                        for (int ii = 0; ii < meshVertexCount; ii += 2, vertexIndex++)
                        {
                            vertices[vertexIndex].x = tempVertices[ii] * scale;
                            vertices[vertexIndex].y = tempVertices[ii + 1] * scale;
                            vertices[vertexIndex].z = z;
                            colors[vertexIndex] = vertColor;
                            uvs[vertexIndex].x = meshUVs[ii];
                            uvs[vertexIndex].y = meshUVs[ii + 1];


                            // Calculate Bounds
                            if (tempVertices[ii] < meshBoundsMin.x)
                                meshBoundsMin.x = tempVertices[ii];
                            else if (tempVertices[ii] > meshBoundsMax.x)
                                meshBoundsMax.x = tempVertices[ii];
                            if (tempVertices[ii + 1] < meshBoundsMin.y)
                                meshBoundsMin.y = tempVertices[ii + 1];
                            else if (tempVertices[ii + 1] > meshBoundsMax.y)
                                meshBoundsMax.y = tempVertices[ii + 1];

                        }
                    }
                }
            }
        }


        // Double buffer mesh.
        Mesh mesh = useMesh1 ? mesh1 : mesh2;

        // Push data from buffers.
        mesh.vertices = vertices;
        mesh.colors32 = colors;
        mesh.uv = uvs;

        // Set Mesh bounds.
        Vector3 meshBoundsExtents = (meshBoundsMax - meshBoundsMin) * scale;    // scaled
        Vector3 meshBoundsCenter = meshBoundsMin + meshBoundsExtents * 0.5f;
        mesh.bounds = new Bounds(meshBoundsCenter, meshBoundsExtents);
        //mesh.RecalculateBounds();

        canvasRenderer.SetMesh(mesh);
        //this.SetVerticesDirty();


        if (mustUpdateMeshStructure)
        {
            int submeshCount = submeshMaterials.Count;
            mesh.subMeshCount = submeshCount;
            for (int i = 0; i < submeshCount; ++i)
                mesh.SetTriangles(submeshes.Items[i].triangles, i);

            /*
			 * TODO: Check fix with a known repro case.
			if (useMesh1)
				lastState.forceUpdateMesh1 = false;
			else
				lastState.forceUpdateMesh2 = false;
			*/
        }

        if (newTriangles && calculateNormals)
        {
            var normals = new Vector3[vertexCount];
            var normal = new Vector3(0, 0, -1);
            for (int i = 0; i < vertexCount; i++)
                normals[i] = normal;
            (useMesh1 ? mesh2 : mesh1).vertices = vertices; // Set other mesh vertices.
            mesh1.normals = normals;
            mesh2.normals = normals;

            if (calculateTangents)
            {
                var tangents = new Vector4[vertexCount];
                var tangent = new Vector3(0, 0, 1);
                for (int i = 0; i < vertexCount; i++)
                    tangents[i] = tangent;
                mesh1.tangents = tangents;
                mesh2.tangents = tangents;
            }
        }

        // Update previous state
        ExposedList<int> attachmentsTriangleCountCurrentMesh;
        ExposedList<bool> attachmentsFlipStateCurrentMesh;
        ExposedList<LastState.AddSubmeshArguments> addSubmeshArgumentsCurrentMesh;
        if (useMesh1)
        {
            attachmentsTriangleCountCurrentMesh = lastState.attachmentsTriangleCountMesh1;
            addSubmeshArgumentsCurrentMesh = lastState.addSubmeshArgumentsMesh1;
            attachmentsFlipStateCurrentMesh = lastState.attachmentsFlipStateMesh1;
            lastState.immutableTrianglesMesh1 = immutableTriangles;
        }
        else
        {
            attachmentsTriangleCountCurrentMesh = lastState.attachmentsTriangleCountMesh2;
            addSubmeshArgumentsCurrentMesh = lastState.addSubmeshArgumentsMesh2;
            attachmentsFlipStateCurrentMesh = lastState.attachmentsFlipStateMesh2;
            lastState.immutableTrianglesMesh2 = immutableTriangles;
        }

        attachmentsTriangleCountCurrentMesh.GrowIfNeeded(attachmentsTriangleCountTemp.Capacity);
        attachmentsTriangleCountCurrentMesh.Count = attachmentsTriangleCountTemp.Count;
        attachmentsTriangleCountTemp.CopyTo(attachmentsTriangleCountCurrentMesh.Items, 0);

        attachmentsFlipStateCurrentMesh.GrowIfNeeded(attachmentsFlipStateTemp.Capacity);
        attachmentsFlipStateCurrentMesh.Count = attachmentsFlipStateTemp.Count;
        attachmentsFlipStateTemp.CopyTo(attachmentsFlipStateCurrentMesh.Items, 0);

        addSubmeshArgumentsCurrentMesh.GrowIfNeeded(addSubmeshArgumentsTemp.Count);
        addSubmeshArgumentsCurrentMesh.Count = addSubmeshArgumentsTemp.Count;
        addSubmeshArgumentsTemp.CopyTo(addSubmeshArgumentsCurrentMesh.Items);

        useMesh1 = !useMesh1;
    }

    protected bool CheckIfMustUpdateMeshStructure(ExposedList<int> attachmentsTriangleCountTemp, ExposedList<bool> attachmentsFlipStateTemp, ExposedList<LastState.AddSubmeshArguments> addSubmeshArgumentsTemp)
    {
        // Check if any mesh settings were changed
        bool mustUpdateMeshStructure =
            immutableTriangles != (useMesh1 ? lastState.immutableTrianglesMesh1 : lastState.immutableTrianglesMesh2);
#if UNITY_EDITOR
        mustUpdateMeshStructure |= !Application.isPlaying;
#endif

        // TODO: Check fix with a known repro case.
        //mustUpdateMeshStructure |= (useMesh1 ? lastState.forceUpdateMesh1 : lastState.forceUpdateMesh2);

        if (mustUpdateMeshStructure)
            return true;

        // Check if any attachments were enabled/disabled
        // or submesh structures has changed
        ExposedList<int> attachmentsTriangleCountCurrentMesh;
        ExposedList<bool> attachmentsFlipStateCurrentMesh;
        ExposedList<LastState.AddSubmeshArguments> addSubmeshArgumentsCurrentMesh;
        if (useMesh1)
        {
            attachmentsTriangleCountCurrentMesh = lastState.attachmentsTriangleCountMesh1;
            addSubmeshArgumentsCurrentMesh = lastState.addSubmeshArgumentsMesh1;
            attachmentsFlipStateCurrentMesh = lastState.attachmentsFlipStateMesh1;
        }
        else
        {
            attachmentsTriangleCountCurrentMesh = lastState.attachmentsTriangleCountMesh2;
            addSubmeshArgumentsCurrentMesh = lastState.addSubmeshArgumentsMesh2;
            attachmentsFlipStateCurrentMesh = lastState.attachmentsFlipStateMesh2;
        }

        // Check attachments
        int attachmentCount = attachmentsTriangleCountTemp.Count;
        if (attachmentsTriangleCountCurrentMesh.Count != attachmentCount)
            return true;

        for (int i = 0; i < attachmentCount; i++)
        {
            if (attachmentsTriangleCountCurrentMesh.Items[i] != attachmentsTriangleCountTemp.Items[i])
                return true;
        }

        // Check flip state
        for (int i = 0; i < attachmentCount; i++)
        {
            if (attachmentsFlipStateCurrentMesh.Items[i] != attachmentsFlipStateTemp.Items[i])
                return true;
        }

        // Check submeshes
        int submeshCount = addSubmeshArgumentsTemp.Count;
        if (addSubmeshArgumentsCurrentMesh.Count != submeshCount)
            return true;

        for (int i = 0; i < submeshCount; i++)
        {
            if (!addSubmeshArgumentsCurrentMesh.Items[i].Equals(ref addSubmeshArgumentsTemp.Items[i]))
                return true;
        }

        return false;
    }

    /** Stores vertices and triangles for a single material. */
    void AddSubmesh(Material submeshMaterial, int startSlot, int endSlot, int triangleCount, int firstVertex, bool lastSubmesh, ExposedList<bool> flipStates)
    {
        int submeshIndex = submeshMaterials.Count;
        submeshMaterials.Add(submeshMaterial);

        if (submeshes.Count <= submeshIndex)
            submeshes.Add(new Submesh());
        else if (immutableTriangles)
            return;

        Submesh submesh = submeshes.Items[submeshIndex];

        int[] triangles = submesh.triangles;
        int trianglesCapacity = triangles.Length;
        if (lastSubmesh && trianglesCapacity > triangleCount)
        {
            // Last submesh may have more triangles than required, so zero triangles to the end.
            for (int i = triangleCount; i < trianglesCapacity; i++)
                triangles[i] = 0;
            submesh.triangleCount = triangleCount;
        }
        else if (trianglesCapacity != triangleCount)
        {
            // Reallocate triangles when not the exact size needed.
            submesh.triangles = triangles = new int[triangleCount];
            submesh.triangleCount = 0;
        }

        if (!renderMeshes && !frontFacing)
        {
            // Use stored triangles if possible.
            if (submesh.firstVertex != firstVertex || submesh.triangleCount < triangleCount)
            {
                submesh.triangleCount = triangleCount;
                submesh.firstVertex = firstVertex;
                int drawOrderIndex = 0;
                for (int i = 0; i < triangleCount; i += 6, firstVertex += 4, drawOrderIndex++)
                {
                    triangles[i] = firstVertex;
                    triangles[i + 1] = firstVertex + 2;
                    triangles[i + 2] = firstVertex + 1;
                    triangles[i + 3] = firstVertex + 2;
                    triangles[i + 4] = firstVertex + 3;
                    triangles[i + 5] = firstVertex + 1;
                }
            }
            return;
        }

        // Store triangles.
        ExposedList<Slot> drawOrder = skeleton.DrawOrder;
        for (int i = startSlot, triangleIndex = 0; i < endSlot; i++)
        {
            Slot slot = drawOrder.Items[i];
            Attachment attachment = slot.attachment;

            bool flip = flipStates.Items[i];

            if (attachment is RegionAttachment)
            {
                if (!flip)
                {
                    triangles[triangleIndex] = firstVertex;
                    triangles[triangleIndex + 1] = firstVertex + 2;
                    triangles[triangleIndex + 2] = firstVertex + 1;
                    triangles[triangleIndex + 3] = firstVertex + 2;
                    triangles[triangleIndex + 4] = firstVertex + 3;
                    triangles[triangleIndex + 5] = firstVertex + 1;
                }
                else
                {
                    triangles[triangleIndex] = firstVertex + 1;
                    triangles[triangleIndex + 1] = firstVertex + 2;
                    triangles[triangleIndex + 2] = firstVertex;
                    triangles[triangleIndex + 3] = firstVertex + 1;
                    triangles[triangleIndex + 4] = firstVertex + 3;
                    triangles[triangleIndex + 5] = firstVertex + 2;
                }

                triangleIndex += 6;
                firstVertex += 4;
                continue;
            }
            int[] attachmentTriangles;
            int attachmentVertexCount;
            var meshAttachment = attachment as MeshAttachment;
            if (meshAttachment != null)
            {
                attachmentVertexCount = meshAttachment.vertices.Length >> 1;
                attachmentTriangles = meshAttachment.triangles;
            }
            else
            {
                var skinnedMeshAttachment = attachment as SkinnedMeshAttachment;
                if (skinnedMeshAttachment != null)
                {
                    attachmentVertexCount = skinnedMeshAttachment.uvs.Length >> 1;
                    attachmentTriangles = skinnedMeshAttachment.triangles;
                }
                else
                    continue;
            }

            if (flip)
            {
                for (int ii = 0, nn = attachmentTriangles.Length; ii < nn; ii += 3, triangleIndex += 3)
                {
                    triangles[triangleIndex + 2] = firstVertex + attachmentTriangles[ii];
                    triangles[triangleIndex + 1] = firstVertex + attachmentTriangles[ii + 1];
                    triangles[triangleIndex] = firstVertex + attachmentTriangles[ii + 2];
                }
            }
            else
            {
                for (int ii = 0, nn = attachmentTriangles.Length; ii < nn; ii++, triangleIndex++)
                {
                    triangles[triangleIndex] = firstVertex + attachmentTriangles[ii];
                }
            }

            firstVertex += attachmentVertexCount;
        }
    }

    public class LastState
    {
        public bool immutableTrianglesMesh1;
        public bool immutableTrianglesMesh2;
        //public bool forceUpdateMesh1, forceUpdateMesh2; // TODO: Check fix with a known repro case.
        public int vertexCount;
        public readonly ExposedList<bool> attachmentsFlipStateTemp = new ExposedList<bool>();
        public readonly ExposedList<bool> attachmentsFlipStateMesh1 = new ExposedList<bool>();
        public readonly ExposedList<bool> attachmentsFlipStateMesh2 = new ExposedList<bool>();
        public readonly ExposedList<int> attachmentsTriangleCountTemp = new ExposedList<int>();
        public readonly ExposedList<int> attachmentsTriangleCountMesh1 = new ExposedList<int>();
        public readonly ExposedList<int> attachmentsTriangleCountMesh2 = new ExposedList<int>();
        public readonly ExposedList<AddSubmeshArguments> addSubmeshArgumentsTemp = new ExposedList<AddSubmeshArguments>();
        public readonly ExposedList<AddSubmeshArguments> addSubmeshArgumentsMesh1 = new ExposedList<AddSubmeshArguments>();
        public readonly ExposedList<AddSubmeshArguments> addSubmeshArgumentsMesh2 = new ExposedList<AddSubmeshArguments>();

        public struct AddSubmeshArguments
        {
            public Material material;
            public int startSlot;
            public int endSlot;
            public int triangleCount;
            public int firstVertex;
            public bool lastSubmesh;

            public AddSubmeshArguments(Material material, int startSlot, int endSlot, int triangleCount, int firstVertex, bool lastSubmesh)
            {
                this.material = material;
                this.startSlot = startSlot;
                this.endSlot = endSlot;
                this.triangleCount = triangleCount;
                this.firstVertex = firstVertex;
                this.lastSubmesh = lastSubmesh;
            }

            public bool Equals(ref AddSubmeshArguments other)
            {
                return
                    !ReferenceEquals(material, null) &&
                    !ReferenceEquals(other.material, null) &&
                    material.GetInstanceID() == other.material.GetInstanceID() &&
                    startSlot == other.startSlot &&
                    endSlot == other.endSlot &&
                    triangleCount == other.triangleCount &&
                    firstVertex == other.firstVertex;
            }
        }
    }
    #endregion

}

