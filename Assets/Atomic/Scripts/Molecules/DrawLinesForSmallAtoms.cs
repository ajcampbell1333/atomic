using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class DrawLinesForSmallAtoms : MonoBehaviour
{
    private Camera _mainCam;
    private CreateAtom[] _atoms;
    [SerializeField] private Material _mat;

    private void Awake()
    {
        _mainCam = GetComponent<Camera>();
        _atoms = FindObjectsOfType(typeof(CreateAtom)) as CreateAtom[];
    }

    void OnPostRender()
    {
        foreach (CreateAtom atom in _atoms)
        {
            if (atom.transform.childCount == 2)
            {
                if (!_mat)
                {
                    Debug.LogError("Please Assign a material on the inspector");
                    return;
                }
                GL.PushMatrix();
                
                //GL.LoadOrtho();

                GL.Begin(GL.LINES);
                _mat.SetPass(0);
                GL.Color(new Color(0, 187, 255));

                Vector3 startPoint = _mainCam.WorldToScreenPoint(atom.transform.GetChild(0).position);
                Vector3 endPoint = _mainCam.WorldToScreenPoint(atom.transform.GetChild(1).position);
                GL.Vertex(atom.transform.GetChild(0).position);
                GL.Vertex(atom.transform.GetChild(1).position);
                
                GL.End();

                GL.PopMatrix();
            }
        }
        
    }
}
