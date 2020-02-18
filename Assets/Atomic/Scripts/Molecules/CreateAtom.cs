using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateAtom : MonoBehaviour
{
    Mesh mesh;
    MeshFilter filter;
    Vector3[] verts;
    int[] triangles;

    /// <summary>
    /// Half-edge data structure:
    /// Key is a vert index, value is a list of verts to which half edges exist
    /// </summary>
    private Dictionary<int, List<int>> _halfEdges;

    GameObject atom;
    ///GameObject[] nodes;
    bool changeCheck, meshIsInitialized;
    [SerializeField] public Material mat;

    [SerializeField] bool testExecuteCreate;

    public bool drawLine;

    // The index of the triangle that should not be drawn in order to make room for the new vert
    int triInterruptIndex;

    private void Awake()
    {
        _halfEdges = new Dictionary<int, List<int>>();
        
    }

    /// <summary>
    
    /// </summary>

    public void CreateMesh()
    {
        if (transform.childCount <= 2) return;
       // nodes = new GameObject[transform.childCount];
        atom = new GameObject();
        filter = atom.AddComponent<MeshFilter>();
        mesh = filter.mesh;
        MeshRenderer renderer = atom.AddComponent<MeshRenderer>();
        renderer.material = mat;
        meshIsInitialized = true;
        UpdateMesh();

        foreach (Transform child in transform)
        {
            GameObject newVertMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(newVertMarker.GetComponent<Collider>());
            newVertMarker.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            newVertMarker.transform.SetParent(child);
            newVertMarker.transform.localPosition = Vector3.zero;
        }
    }

    public void UpdateMesh()
    {
        if (!meshIsInitialized || transform.childCount <= 2) return;
        
        mesh.Clear();
        verts = new Vector3[3 * transform.childCount];
        

        Vector3 center = CalculateCentroid();

        foreach (Transform child in transform)
        {
            for (int vertAtChild = 0; vertAtChild < 3; vertAtChild++)
            {
                verts[vertAtChild + (child.GetSiblingIndex() * 3)] = child.position;
            }
        }

        if (transform.childCount <= 2) return;
        else if (transform.childCount == 3)
        {
            /// 1b) If 3 verts, create a triangles array with 3 indices and draw a tri.
            triangles = new int[3];
            SetUpFirstTri();
        }
        else if (transform.childCount == 4)
        {
            /// 1c) If 4 verts, recreate the triangles array with 9 more indices than before (i.e. add three more tris).
            triangles = new int[12];
            //bool fourthPointIsOnWhichSide = Vector3.Dot(Vector3.Cross(verts[0] - verts[3], verts[0] - verts[6]), verts[0] - verts[9]) < 0;
            SetUpFirstTri();
            //triangles[3] = 10;
            //triangles[4] = 4;
            //triangles[5] = 1;

                //triangles[3] = 9;
                //triangles[4] = (fourthPointIsOnWhichSide) ? 1 : 4;
                //triangles[5] = (fourthPointIsOnWhichSide) ? 4 : 1;

            triangles[6] = 10;
            triangles[7] = 8;
            triangles[8] = 1;
            
                //triangles[7] = (fourthPointIsOnWhichSide) ? 5 : 7;
                //triangles[8] = (fourthPointIsOnWhichSide) ? 7 : 5;
            
            //triangles[9] = 11;
            //triangles[10] = 1;
            //triangles[11] = 8;
            
                //triangles[9] = 9;
                //triangles[10] = (fourthPointIsOnWhichSide) ? 8 : 1;
                //triangles[11] = (fourthPointIsOnWhichSide) ? 1 : 8;
        }        
        else if (transform.childCount > 4)
        {
            /// 1d) If more verts, recreate the triangles array with 6 more indices than before (i.e. replace one tri and add two more).
            triangles = new int[3 * transform.childCount];

            /// 2) The triangleIndex selected when the new vert was connected is the one to replace.
            /// 3) Reassign its indices, and then add the two new ones at the end of the array.
            /// 4) Don't forget to update the half-edge structure every time (6 new.
        }





        //for (int childIndex = 3; childIndex < transform.childCount; childIndex++ )
        //{
        //    // if [childIndex]th child is not last child, then calculate nearest 3 to index
        //    triangles[childIndex*3] = childIndex*3;
        //    triangles[childIndex*3 + 1] = childIndex*3 + 1;
        //    triangles[childIndex * 3 + 2] = childIndex * 3 + 2;
        //}

        //triangles[0] = 0; // 0
        //triangles[1] = 3; // 1
        //triangles[2] = 6; // 2




        //triangles[3] = 4; // 1
        //triangles[4] = 10; // 3
        //triangles[5] = 7; // 2
        //triangles[6] = 1; // 0
        //triangles[7] = 9; // 3
        //triangles[8] = 5; // 1
        //triangles[9] = 8; // 0
        //triangles[10] = 11; // 2
        //triangles[11] = 1; // 3

        mesh.vertices = verts;
        mesh.triangles = triangles;
        mesh.normals = new Vector3[verts.Length];

        for (int i = 0; i < verts.Length; i++)
            mesh.normals[i] = verts[i] - center;
        
        mesh.RecalculateNormals();
        filter.mesh = mesh;
    }

    public void AddVert(GameObject newNode)
    {

    }

    public void SetUpFirstTri()
    {
        for (int i = 0; i < 3; i++)
        {
            verts[i] = transform.GetChild(i).position;
            triangles[i] = i * 3; // 0
        }
    }

    private void Update()
    {
        if (testExecuteCreate)
        {
            testExecuteCreate = false;
            CreateMesh();
        }

       
        foreach (Transform child in transform)
        {
            if (transform.hasChanged)
            {
                changeCheck = true;
            }
        }
        
        

        if (changeCheck)
        {
            UpdateMesh();
            changeCheck = false;
        }


        // Draw a connection line if there aren't enough children for a shape

        //_drawLine = (transform.childCount == 2) ? true : false;

    }

    

    Vector3 CalculateCentroid()
    {
        Vector3 avgPos = Vector3.zero;
        foreach (Transform child in transform)
        {
            avgPos += child.position;
        }
        avgPos /= transform.childCount;
        return avgPos;
    }
}
