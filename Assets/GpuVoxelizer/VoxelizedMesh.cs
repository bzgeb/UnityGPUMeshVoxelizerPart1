#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Profiling;

[ExecuteAlways]
public class VoxelizedMesh : MonoBehaviour
{
    [SerializeField] MeshFilter _meshFilter;
    [SerializeField] MeshCollider _meshCollider;
    [SerializeField] float _halfSize = 0.05f;
    [SerializeField] Vector3 _boundsMin;

    [SerializeField] Material _gridPointMaterial;
    [SerializeField] int _gridPointCount;

    [SerializeField] ComputeShader _voxelizeComputeShader;
    ComputeBuffer _voxelPointsBuffer;

    [SerializeField] bool _drawDebug;

    static readonly int LocalToWorldMatrix = Shader.PropertyToID("_LocalToWorldMatrix");
    static readonly int BoundsMin = Shader.PropertyToID("_BoundsMin");
    static readonly int VoxelGridPoints = Shader.PropertyToID("_VoxelGridPoints");

    Vector4[] _gridPoints;

    void OnRenderObject()
    {
        if (!_drawDebug) return;

        VoxelizeMeshWithGPU();

        _gridPointMaterial.SetMatrix(LocalToWorldMatrix, transform.localToWorldMatrix);
        _gridPointMaterial.SetVector(BoundsMin, new Vector4(_boundsMin.x, _boundsMin.y, _boundsMin.z, 0.0f));
        _gridPointMaterial.SetBuffer(VoxelGridPoints, _voxelPointsBuffer);
        _gridPointMaterial.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Points, 1, _gridPointCount);
    }

    void OnDestroy()
    {
        _voxelPointsBuffer.Dispose();
    }

    void VoxelizeMeshWithGPU()
    {
        Profiler.BeginSample("Voxelize Mesh (GPU)");

        Bounds bounds = _meshCollider.bounds;
        _boundsMin = transform.InverseTransformPoint(bounds.min);

        Vector3 voxelCount = bounds.extents / _halfSize;
        int xGridSize = Mathf.CeilToInt(voxelCount.x);
        int yGridSize = Mathf.CeilToInt(voxelCount.y);
        int zGridSize = Mathf.CeilToInt(voxelCount.z);

        _voxelPointsBuffer?.Dispose();
        _voxelPointsBuffer = new ComputeBuffer(xGridSize * yGridSize * zGridSize, 4 * sizeof(float));
        if (_gridPoints == null || _gridPoints.Length != xGridSize * yGridSize * zGridSize)
        {
            _gridPoints = new Vector4[xGridSize * yGridSize * zGridSize];
        }
        _voxelPointsBuffer.SetData(_gridPoints);

        var voxelizeKernel = _voxelizeComputeShader.FindKernel("VoxelizeMesh");
        _voxelizeComputeShader.SetInt("_GridWidth", xGridSize);
        _voxelizeComputeShader.SetInt("_GridHeight", yGridSize);
        _voxelizeComputeShader.SetInt("_GridDepth", zGridSize);

        _voxelizeComputeShader.SetFloat("_CellHalfSize", _halfSize);

        _voxelizeComputeShader.SetBuffer(voxelizeKernel, VoxelGridPoints, _voxelPointsBuffer);

        _voxelizeComputeShader.SetVector(BoundsMin, _boundsMin);

        _voxelizeComputeShader.GetKernelThreadGroupSizes(voxelizeKernel, out uint xGroupSize, out uint yGroupSize,
            out uint zGroupSize);

        _voxelizeComputeShader.Dispatch(voxelizeKernel,
            Mathf.CeilToInt(xGridSize / (float) xGroupSize),
            Mathf.CeilToInt(yGridSize / (float) yGroupSize),
            Mathf.CeilToInt(zGridSize / (float) zGroupSize));
        _gridPointCount = _voxelPointsBuffer.count;

        Profiler.EndSample();
    }

#if UNITY_EDITOR
    void Reset()
    {
        _meshFilter = GetComponent<MeshFilter>();
        if (TryGetComponent(out MeshCollider meshCollider))
        {
            _meshCollider = meshCollider;
        }
        else
        {
            _meshCollider = gameObject.AddComponent<MeshCollider>();
        }

        var basePath = "Assets/GpuVoxelizer/";
        _gridPointMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{basePath}Materials/GridPointMaterial.mat");
        _voxelizeComputeShader =
            AssetDatabase.LoadAssetAtPath<ComputeShader>($"{basePath}ComputeShaders/VoxelizeMesh.compute");
    }
#endif
}