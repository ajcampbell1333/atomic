using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriangleGenerator : MonoBehaviour
{
    public bool ExtrusionUnitTest;

    public bool isTrackingChanges = true;
    public MoleculeFace targetFace;
    public List<MoleculeFace> faces;

    [SerializeField] private Material _triangleMat;
    

    private Vector3[] verts;

    private void Awake()
    {
        faces = new List<MoleculeFace>();
        verts = new Vector3[3];
        TriangleCreationUnitTest01();
    }

    private void LateUpdate()
    {
        if (ExtrusionUnitTest)
        {
            Extrude(transform.GetChild(3));
            ExtrusionUnitTest = false;
        }

        if (isTrackingChanges && AnyTransformChanged())
        {
            UpdateTriangle();
            ResetAllChangeMonitors();
        }
    }

    #region helper methods
    public void Extrude(Transform newPoint)
    {
        MoleculeFace[] faces = new MoleculeFace[3];
        for (int i = 0; i < 3; i++)
        {
            if (i >= 1)
            {
            }
            GameObject newTriangleParent = new GameObject("Triangle");
            verts[0] = targetFace.edges[i].verts[0];
            verts[1] = targetFace.edges[i].verts[1];
            verts[2] = newPoint.position;
            CreateTriangle(verts, newTriangleParent.transform);
        }

        for (int j = 0; j < 3; j++)
        {
            faces[j].edges[0].neighbor = targetFace;
            faces[j].edges[2].neighbor = faces[(j - 1 < 0) ? 2 : j - 1];
            faces[j].edges[1].neighbor = faces[(j + 1 < 3) ? j + 1 : 0];
        }

    }

    public bool AnyTransformChanged()
    {
        foreach (Transform child in transform)
            if (child.hasChanged)
                return true;
        return transform.hasChanged;
    }

    public void ResetAllChangeMonitors()
    {
        foreach (Transform child in transform)
            child.hasChanged = false;
        transform.hasChanged = false;
    }

    public void TriangleCreationUnitTest01()
    {
        for (int i = 0; i < transform.childCount; i++)
            verts[i] = transform.GetChild(i).localPosition;

        CreateTriangle(verts);
    }

    public void UpdateTriangle()
    {
        for (int i = 0; i < 3; i++)
        {
            verts[i] = transform.GetChild(i).localPosition;
        }
        targetFace.mesh.vertices = verts;
        targetFace.mesh.RecalculateNormals();
    }

    public void CreateTriangle(Vector3[] verts)
    {
        MoleculeFace currentFace = new MoleculeFace(verts, _triangleMat);
        faces.Add(currentFace);
        if (faces.Count == 1)
            targetFace = currentFace;
        currentFace.meshObject.transform.SetParent(transform);
    }

    public void CreateTriangle(Vector3[] verts, Transform parent)
    {
        MoleculeFace currentFace = new MoleculeFace(verts, _triangleMat);
        faces.Add(currentFace);
        if (faces.Count == 1)
            targetFace = currentFace;
        currentFace.meshObject.transform.SetParent(parent);
    }
    #endregion helper methods
}

public struct MoleculeFace
{
    public MoleculeFace(Material _mat)
    {
        mesh = null;
        edges = null;
        meshObject = null;
        collider = null;
        material = _mat;
    }

    public MoleculeFace(Vector3[] verts, Material _mat)
    {
        mesh = new Mesh();
        edges = new SharedEdge[3];
        for (int i = 0; i < 3; i++)
        {
            int finishIndex = (i + 1 < 3) ? i + 1 : 0;
            edges[i] = new SharedEdge(_mat, verts[i], verts[finishIndex]);
        }
        mesh.vertices = verts;
        mesh.triangles = new int[] { 0,1,2 };
        mesh.RecalculateNormals();
        meshObject = new GameObject(new Guid().ToString());
        MeshFilter mf = meshObject.AddComponent<MeshFilter>();
        mf.mesh = mesh;
        try {
            collider = meshObject.AddComponent<MeshCollider>();

        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
            collider = null;
        }
        MeshRenderer renderer = meshObject.AddComponent<MeshRenderer>();
        material = renderer.material = _mat;
    }
    public Mesh mesh;
    public SharedEdge[] edges;
    public GameObject meshObject;
    public MeshCollider collider;
    public Material material;
}

public struct SharedEdge
{
    public SharedEdge(Material _mat, Vector3 vert1, Vector3 vert2)
    {
        neighbor = new MoleculeFace(_mat);
        verts = new Vector3[2];
        verts[0] = vert1;
        verts[1] = vert2;
    }

    public SharedEdge(MoleculeFace _neighbor, Vector3 vert1, Vector3 vert2)
    {
        neighbor = _neighbor;
        verts = new Vector3[2];
        verts[0] = vert1;
        verts[1] = vert2;
    }

    public MoleculeFace neighbor;
    public Vector3[] verts;
}
