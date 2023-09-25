using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeeThroughObject : MonoBehaviour
{
    public static int PosID = Shader.PropertyToID("_PlayerPosition");
    public static int SizeID = Shader.PropertyToID("_Size");
    public static int OpacityID = Shader.PropertyToID("_Opacity");

    public Material groundMat;
    public Camera camera;
    public LayerMask mask;
    public List<GameObject> listaTerreno = new List<GameObject>();

    // Update is called once per frame
    void Update()
    {
        var dir = camera.transform.position - transform.position;
        var ray = new Ray(transform.position, dir.normalized);

        //float dist = Vector3.Distance(transform.position, camera.transform.position);

        //if (Physics.Raycast(ray, dist, mask))
        //{
        //    groundMat.SetFloat(SizeID, 1);
        //}
        //else
        //{
        //    groundMat.SetFloat(SizeID, 0);
        //}
        
        if(Physics.Raycast(ray, out var hit, mask))
        {
            var terra = hit.collider.gameObject;

            var father = terra.GetComponentInParent<Transform>();

            if (father.position.y < transform.position.y) return;
            
            listaTerreno.Add(terra);
            
        }
        
        

        foreach(var terra in listaTerreno) 
        {
            var mat = groundMat;
        
            mat.SetFloat(SizeID, 1);

            terra.GetComponent<Renderer>().material = mat;
        }

        var view = camera.WorldToViewportPoint(transform.position);
        groundMat.SetVector(PosID, view);
    }
}

