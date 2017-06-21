using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using MiniJSON;

using GoShared;

namespace GoMap
{
	[ExecuteInEditMode]
	public abstract class GOTile : MonoBehaviour
	{
		public Coordinates tileCenter;
		public Vector2 tileCoordinates;
		public string tileUrl;

		[HideInInspector] public float diagonalLenght;
		[HideInInspector] public List<Vector3> vertices;
		[HideInInspector] public object mapData;
		[HideInInspector] public GOMap map;

		ParseJob job;
		IList buildingsIds = new List<object>();

		bool TileLoaded = false;

		public abstract IEnumerator BuildTile (IDictionary mapData, GOLayer layer, bool delayedLoad);
		public abstract IEnumerator ParseTileData (object m, Coordinates tilecenter, int zoom, GOLayer[] layers, bool delayedLoad, List <string> layerNames);
		public abstract string GetTileUrl ();


		#region BUILDINGS

		private List <Vector3> buildingCenters = new List<Vector3>();
		private float mdc = 60; // Group buildings every 50meters
		public GameObject findNearestCenter (Vector3 center, GameObject parent, Material material) {

			string name = "Container " + center.x + " "+center.y+ " "+center.z;
			foreach (Vector3 c in buildingCenters) {
				float d = Mathf.Abs(Vector3.Distance (center, c));
				if (d <= mdc) {
					string n = "Container " + c.x + " "+c.y+ " "+c.z;
					Transform child = parent.transform.Find (n);
					if (child != null)
						return child.gameObject;
				}
			}

			GameObject container = new GameObject (name);
			//			container.transform.localPosition = center;
			container.transform.parent = parent.transform;
			container.AddComponent<GOMatHolder> ().material = material;
			buildingCenters.Add (center);
			return container;
		}

		public Material GetMaterial (GORenderingOptions rendering, Vector3 center) {
		
			if (rendering.materials.Length > 0) {
				float seed = center.x * center.z * 100;
				System.Random rnd = new System.Random ((int)seed);
				int pick = rnd.Next (0, rendering.materials.Length);
				Material material = rendering.materials [pick];
				return material;
			} else
				return rendering.material;
		}

		void OnDestroy() {
			removeIds ();
			//			Debug.Log ("Destroy tile: "+gameObject.name);
		}

		public bool idCheck (object id, GOLayer layer) {
			if (map.buildingsIds.Contains (id)) {
				//				Debug.Log ("id already created");
				return false;
			} else {
				//					Debug.Log ("id added");
				buildingsIds.Add(id);
				map.buildingsIds.Add(id);
				return true;
			}
		}

		private void removeIds () {
			foreach (object id in buildingsIds) {
				map.buildingsIds.Remove (id);
			}
		}

		#endregion

		public void AddObjectToLayerMask (GOLayer layer, GameObject obj) {
			LayerMask mask = LayerMask.NameToLayer (layer.name);
			if (mask.value > 0 && mask.value < 31) {
				obj.layer = LayerMask.NameToLayer (layer.name);
			} else {
				Debug.LogWarning ("[GOMap] Please create layer masks before running GoMap. A layer mask must have the same name declared in GoMap inspector, for example \""+layer.name+"\".");
			}
		}

		#region Network

		public IEnumerator LoadTileData(GOMap m, Coordinates tilecenter, int zoom, GOLayer[] layers, bool delayedLoad) {

			map = m;

			tileCoordinates = tileCenter.tileCoordinates (map.zoomLevel);

			if (Application.isPlaying) {
				yield return StartCoroutine (DownloadData (m, tileCenter, zoom, layers, delayedLoad));
				List <string> layerNames = map.layerNames();
				yield return StartCoroutine(ParseTileData(map,tileCenter,zoom,layers,delayedLoad,layerNames));
			} 
			else {
				GORoutine.start (DownloadData (m, tileCenter, zoom, layers, delayedLoad), this);
			}
		}

		#if UNITY_EDITOR
		public void Update () {

			if (Application.isPlaying || TileLoaded || !map)
				return;
			
			if(mapData!=null)
			{
				TileLoaded = true;
				GORoutine.start (ParseTileData(map,tileCenter,map.locationManager.zoomLevel,map.layers,false,map.layerNames()),this);
			}
		}
		#endif


		public virtual IEnumerator DownloadData(object m, Coordinates tilecenter, int zoom, GOLayer[] layers, bool delayedLoad) {

			string completeUrl = GetTileUrl();
			string filename = "[" + map.mapType.ToString () + "]" + gameObject.name;

			return GOUrlRequest.getRequest (this, completeUrl, map.useCache, filename, (byte[] bytes, string text, string error) => {

				if (error == null) {
					mapData = bytes;
					tileUrl = completeUrl;

					#if UNITY_EDITOR
					if (!Application.isPlaying)
						Update();
					#endif
				} 
			});

		}


		public IEnumerator ParseJson (string data) {

			job = new ParseJob();
			job.InData = data;
			job.Start();

			yield return StartCoroutine(job.WaitFor());
		}

		#endregion

	}
}
