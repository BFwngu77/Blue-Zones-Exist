using UnityEngine;
using System.Collections;
using GoMap;

using GoShared;
using System;
using UnityEngine.Events;


public class MoveAvatar : MonoBehaviour {

	public LocationManager locationManager;
	public GameObject avatarFigure;

	public AvatarAnimationState animationState = AvatarAnimationState.Idle;
	[HideInInspector] public float dist;
	public enum AvatarAnimationState{
		Idle, 
		Walk,
		Run
	};
	public GOAvatarAnimationStateEvent OnAnimationStateChanged;

	// Use this for initialization
	void Start () {

		locationManager.onOriginSet.AddListener((Coordinates) => {OnOriginSet(Coordinates);});
		locationManager.onLocationChanged.AddListener((Coordinates) => {OnLocationChanged(Coordinates);});
	}

	void OnOriginSet (Coordinates currentLocation) {

		//Position
		Vector3 currentPosition = currentLocation.convertCoordinateToVector ();
		currentPosition.y = transform.position.y;

		transform.position = currentPosition;

	}

	void OnLocationChanged (Coordinates currentLocation) {

		Vector3 lastPosition = transform.position;

		//Position
		Vector3 currentPosition = currentLocation.convertCoordinateToVector ();
		currentPosition.y = transform.position.y;

		if (lastPosition == Vector3.zero) {
			lastPosition = currentPosition;
		}

//		transform.position = currentPosition;
//		rotateAvatar (lastPosition);

		moveAvatar (lastPosition,currentPosition);

	}

	void moveAvatar (Vector3 lastPosition, Vector3 currentPosition) {

		StartCoroutine (move (lastPosition,currentPosition,0.5f));
	}

	private IEnumerator move(Vector3 lastPosition, Vector3 currentPosition, float time) {

		float elapsedTime = 0;
		Vector3 targetDir = currentPosition-lastPosition;
		Quaternion finalRotation = Quaternion.LookRotation (targetDir);

		while (elapsedTime < time)
		{
			transform.position = Vector3.Lerp(lastPosition, currentPosition, (elapsedTime / time));
			avatarFigure.transform.rotation = Quaternion.Lerp(avatarFigure.transform.rotation, finalRotation,(elapsedTime / time));

			elapsedTime += Time.deltaTime;

			dist = Vector3.Distance (lastPosition, currentPosition);

			AvatarAnimationState state = AvatarAnimationState.Idle; 

			if (dist > 4)
				state = AvatarAnimationState.Run;
			else state = AvatarAnimationState.Walk;

			if (state != animationState) {

				animationState = state;
				OnAnimationStateChanged.Invoke(animationState);
			}

			yield return new WaitForEndOfFrame();
		}

		animationState = AvatarAnimationState.Idle;
		OnAnimationStateChanged.Invoke(animationState);

//		avatarFigure.transform.rotation = finalRotation;
	}
		
	void rotateAvatar(Vector3 lastPosition) {
	
		//Orient Avatar
		Vector3 targetDir = transform.position-lastPosition;

		if (targetDir != Vector3.zero) {
			avatarFigure.transform.rotation = Quaternion.Slerp(
				avatarFigure.transform.rotation,
				Quaternion.LookRotation(targetDir),
				Time.deltaTime * 10.0f
			);
		}
	}


}

[Serializable]
public class GOAvatarAnimationStateEvent : UnityEvent <MoveAvatar.AvatarAnimationState> {


}
