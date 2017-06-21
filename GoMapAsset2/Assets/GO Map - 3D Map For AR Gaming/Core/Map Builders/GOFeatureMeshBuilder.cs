using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;

namespace GoMap
{

    public class GOFeatureMeshBuilder
    {
		GOFeature feature;
		public Mesh mesh;
		public Mesh mesh2D;
		public Vector3 center;

		public GOFeatureMeshBuilder (GOFeature f) {

			feature = f;
			if (feature.goFeatureType == GoMap.GOFeature.GOFeatureType.Polygon || feature.goFeatureType == GoMap.GOFeature.GOFeatureType.MultiPolygon)
				center = feature.convertedGeometry.Aggregate((acc, cur) => acc + cur) / feature.convertedGeometry.Count;

		}

		#region Builders

		public void BuildLine(GameObject line, GOLayer layer, GORenderingOptions renderingOptions , GOMap map)
        {
			if (feature.convertedGeometry.Count == 2 && feature.convertedGeometry[0].Equals(feature.convertedGeometry[1])) {
				return;
			}

			if (renderingOptions.tag.Length > 0) {
				line.tag = renderingOptions.tag;
			}

			if (renderingOptions.material)
				renderingOptions.material.renderQueue = -(int)feature.sort;
			if (renderingOptions.outlineMaterial)
				renderingOptions.outlineMaterial.renderQueue = -(int)feature.sort;
			
			GOLineMesh lineMesh = new GOLineMesh (feature.convertedGeometry);
			lineMesh.width = renderingOptions.lineWidth;
			lineMesh.load (line);
			mesh = lineMesh.mesh;
			line.GetComponent<Renderer>().material = renderingOptions.material;

			Vector3 position = line.transform.position;
			position.y = feature.y;

			#if GOLINK
			//[GOLINK] Trigger GOMap Tile creation (This requires both GoMap and GoTerrain)
			if (renderingOptions.polygonHeight > 0) {
				int offset = 5;
				line.GetComponent<MeshFilter> ().sharedMesh = SimpleExtruder.Extrude (line.GetComponent<MeshFilter> ().sharedMesh, line, renderingOptions.polygonHeight + offset);
				position.y -= offset;
			}
			#else

			#endif

			line.transform.position = position;

			if (renderingOptions.outlineMaterial != null) {
				GameObject outline = CreateRoadOutline (line,renderingOptions.outlineMaterial, renderingOptions.lineWidth + layer.defaultRendering.outlineWidth);
				if (layer.useColliders)
					outline.AddComponent<MeshCollider> ().sharedMesh = outline.GetComponent<MeshFilter> ().sharedMesh;

				outline.layer = line.layer;
				outline.tag = line.tag;
				
			} else if (layer.useColliders) {
//				Mesh m = gameObject.GetComponent<MeshFilter> ().sharedMesh;
				line.AddComponent<MeshCollider> ();
			}
        }

		public GameObject BuildPolygon(GOLayer layer, float height)
		{

			GameObject polygon = new GameObject();

			Poly2Mesh.Polygon poly = new Poly2Mesh.Polygon();
			poly.outside = feature.convertedGeometry;
			if (feature.clips != null ) {
				foreach (IList clipVerts in feature.clips) {
					poly.holes.Add(GOFeature.CoordsToVerts(clipVerts));
				}
			}

			MeshFilter filter = polygon.AddComponent<MeshFilter>();
			polygon.AddComponent(typeof(MeshRenderer));

			try {
				mesh = Poly2Mesh.CreateMesh (poly);
			} catch {
				
			}

			if (mesh) {
				Vector2[] uvs = new Vector2[mesh.vertices.Length];
				for (int i=0; i < uvs.Length; i++) {
					uvs[i] = new Vector2(mesh.vertices[i].x, mesh.vertices[i].z);
				}
				mesh.uv = uvs;

				mesh2D = Mesh.Instantiate(mesh);

				if (height > 0) {
					mesh = SimpleExtruder.Extrude (mesh, polygon, height);
				}


			}
			filter.sharedMesh = mesh;

			if (layer.useColliders)
				polygon.AddComponent<MeshCollider>().sharedMesh = mesh;



			return polygon;

		}


		#endregion

		#region LINE UTILS

		GameObject CreateRoadOutline (GameObject line, Material material, float width) {

			GameObject outline = new GameObject ("outline");
			outline.transform.parent = line.transform;

			material.renderQueue = -((int)feature.sort-1);

			GOLineMesh lineMesh = new GOLineMesh (feature.convertedGeometry);
			lineMesh.width = width;
			lineMesh.load (outline);

			Vector3 position = outline.transform.position;
			position.y = -0.039f;
			outline.transform.localPosition = position;

			outline.GetComponent<Renderer>().material = material;

			return outline;
		}

		//[GOLINK] GOTerrain link (This requires GOTerrain! https://www.assetstore.unity3d.com/#!/content/84198) 
		#if GOLINK
		public List<Vector3> BreakLine (List<Vector3> verts, GoTerrain.GOTerrain terrain) {

			int treshold = 25;
			List<Vector3> brokenVerts = new List <Vector3> ();
			for (int i = 0; i<verts.Count-1; i++) {
				
				float d = Vector3.Distance (verts [i], verts [i + 1]);
				if (d > treshold) {
					for (int j = 0; j < d; j += treshold) {
						Vector3 P = LerpByDistance (verts [i], verts [i + 1], j);
						P.y = terrain.FindAltitudeForVector (P);
						brokenVerts.Add (P);
					}
				} else {
					Vector3 P = verts [i];
					P.y = terrain.FindAltitudeForVector (P);
					brokenVerts.Add(P);
				}

			}
			Vector3 Pn = verts [verts.Count - 1];
			Pn.y = terrain.FindAltitudeForVector (Pn);
			brokenVerts.Add (Pn);
			return brokenVerts;
		}

		public Vector3 LerpByDistance(Vector3 A, Vector3 B, float x)
		{
			Vector3 P = x * Vector3.Normalize(B - A) + A;
			return P;
		}
		#endif

		#endregion

		#region POLYGON UTILS

		public GameObject CreateRoof (){

			GameObject roof = new GameObject();
			MeshFilter filter = roof.AddComponent<MeshFilter>();
			roof.AddComponent(typeof(MeshRenderer));
			filter.mesh = mesh2D;
			return roof;
		}

		public static string VectorListToString (List<Vector3> list) {

			list =  new HashSet<Vector3>(list).ToList();
			string s = "";
			foreach (Vector3 v in list) {
				s += v.ToString() + " ";
			}
			return s;

		}

		#endregion

    }




}
