using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Pun.UtilityScripts;

/// <summary>
/// The object attached to the camera that Slerps the camera and fires bullets
/// </summary>
public class SmoothMouseLook : MonoBehaviour {

	public enum RotationAxes { MouseXAndY = 0, MouseX = 1, MouseY = 2 }
	public GameObject parent;
	private playerController pc;
    private playerBlasterController pbc;
	public RotationAxes axes = RotationAxes.MouseXAndY;
	public float sensitivityX = 15F;
	public float sensitivityY = 15F;

	public float minimumX = -360F;
	public float maximumX = 360F;

	public float minimumY = -60F;
	public float maximumY = 60F;

	float rotationX = 0F;
	float rotationY = 0F;

	private List<float> rotArrayX = new List<float>();
	float rotAverageX = 0F;	

	private List<float> rotArrayY = new List<float>();
	float rotAverageY = 0F;

	private float frameCounter = 5;

    private float interactLength = 30f;

	Quaternion originalRotation;

	public GameController gc;
	private Vector3 cameraPos;

	//does this have a player connected
	public bool hasPlayer;

    //is the crosshair still on something
    public bool isInteracting;

    //Are the crosshairs on a player
    public bool foundPlayer;

    private float gunDist;
    //A mask used to allow bullets to go through blasters, I hardcoded an int but if
    //more layers are ever added, this will need to change
    private int layerMask = 511;

    private Vector3 crossHairWorldPos;
    private Vector3 crossHairScreenPos;

    private Camera cam;
    private float camWidth;
    private float camHeight;

    private bool gameOver;

    [SerializeField]
    private GameObject pregameCanvas;

	void Update ()
	{
        if (!gameOver)
        {
            //used to interact with objects
            RaycastHit interact;
            //used to see if a player is in the crosshairs
            RaycastHit playerFinder;

            crossHairWorldPos = cam.ScreenToWorldPoint(crossHairScreenPos);

            if (Physics.Raycast(crossHairWorldPos, transform.TransformDirection(Vector3.forward), out interact, interactLength))
            {
                if (interact.collider.tag.Equals("item"))
                {
                    isInteracting = true;
                    if (hasPlayer)
                    {
                        pc.foundObject();
                        if (Input.GetMouseButtonDown(0))
                        {
                            Blaster blaster = interact.collider.gameObject.GetComponent<Blaster>();
                            if (!blaster.getIsOwned() && blaster.getBlasterType() != 4)
                            {
                                pbc.addBlaster(blaster);
                            }
                        }

                    }
                    //if for some reason a PC isn't connected
                    else
                    {
                        Debug.Log("No player controller attached");
                    }
                }
                else
                {
                    isInteracting = false;
                }

            }
            else
            {
                isInteracting = false;
            }


            // Does the ray intersect any objects excluding the player layer
            if (Physics.Raycast(crossHairWorldPos, transform.TransformDirection(Vector3.forward), out playerFinder, gunDist, layerMask))
            {
                if (playerFinder.collider.tag.Equals("Player"))
                {
                    if (playerFinder.collider.GetComponent<playerController>().checkIfRedTeam() != pc.checkIfRedTeam())
                    {
                        foundPlayer = true;

                        if (hasPlayer)
                        {
                            pc.foundPlayer();
                        }
                        //In a perfect world, this line never runs
                        else
                        {
                            Debug.Log("No player controller attached");
                        }
                    }
                }
                else if (playerFinder.collider.tag.Equals("AI"))
                {
                    if (playerFinder.collider.GetComponent<aiPlayerController>().checkIfRedTeam() != pc.checkIfRedTeam())
                    {
                        foundPlayer = true;

                        if (hasPlayer)
                        {
                            pc.foundPlayer();
                        }
                        else
                        {
                            Debug.Log("No player controller attached");
                        }
                    }
                }

                else
                {
                    foundPlayer = false;
                }
                //Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);
                //Debug.Log("Did Hit");
            }
            else
            {
                foundPlayer = false;
            }


            if (!isInteracting && !foundPlayer)
            {
                if (hasPlayer)
                {
                    if (pc.isCrossHairChanged())
                    {
                        pc.foundNothing();
                    }
                }
                else
                {
                    //Debug.Log("No player controller attached");
                }
            }

            //Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 15, Color.yellow);
            if (gc == null)
            {
                // Debug.Log("Null gc");
                gc = parent.GetComponentInChildren<GameController>();
                if (gc == null)
                {
                    //Debug.Log("Could not locate GameController");
                }
            }
            else
            {
                if (!gc.getQuestionMode())
                {
                    if (axes == RotationAxes.MouseXAndY)
                    {
                        rotAverageY = 0f;
                        rotAverageX = 0f;

                        rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
                        rotationX += Input.GetAxis("Mouse X") * sensitivityX;

                        //rotationX = ClampAngle(rotationX, minimumX, maximumX);
                        rotationY = ClampAngle(rotationY, minimumY, maximumY);

                        rotArrayY.Add(rotationY);
                        rotArrayX.Add(rotationX);

                        if (rotArrayY.Count >= frameCounter)
                        {
                            rotArrayY.RemoveAt(0);
                        }
                        if (rotArrayX.Count >= frameCounter)
                        {
                            rotArrayX.RemoveAt(0);
                        }

                        for (int j = 0; j < rotArrayY.Count; j++)
                        {
                            rotAverageY += rotArrayY[j];
                        }
                        for (int i = 0; i < rotArrayX.Count; i++)
                        {
                            rotAverageX += rotArrayX[i];
                        }

                        rotAverageY /= rotArrayY.Count;
                        rotAverageX /= rotArrayX.Count;

                        rotAverageY = ClampAngle(rotAverageY, minimumY, maximumY);
                        //rotAverageX = ClampAngle(rotAverageX, minimumX, maximumX);

                        Quaternion yQuaternion = Quaternion.AngleAxis(rotAverageY, Vector3.left);
                        Quaternion xQuaternion = Quaternion.AngleAxis(rotAverageX, Vector3.up);

                        //Quaternion yQuaternion = Quaternion.AngleAxis(rotationY, Vector3.left);
                        //Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);

                        transform.localRotation = originalRotation * yQuaternion;
                        parent.transform.localRotation = originalRotation * xQuaternion;

                    }
                    else if (axes == RotationAxes.MouseX)
                    {
                        rotAverageX = 0f;

                        rotationX += Input.GetAxis("Mouse X") * sensitivityX;

                        rotArrayX.Add(rotationX);

                        if (rotArrayX.Count >= frameCounter)
                        {
                            rotArrayX.RemoveAt(0);
                        }
                        for (int i = 0; i < rotArrayX.Count; i++)
                        {
                            rotAverageX += rotArrayX[i];
                        }
                        rotAverageX /= rotArrayX.Count;

                        //rotAverageX = ClampAngle(rotAverageX, minimumX, maximumX);

                        Quaternion xQuaternion = Quaternion.AngleAxis(rotAverageX, Vector3.up);
                        transform.localRotation = originalRotation * xQuaternion;
                    }
                    else
                    {
                        rotAverageY = 0f;

                        rotationY += Input.GetAxis("Mouse Y") * sensitivityY;

                        rotArrayY.Add(rotationY);

                        if (rotArrayY.Count >= frameCounter)
                        {
                            rotArrayY.RemoveAt(0);
                        }
                        for (int j = 0; j < rotArrayY.Count; j++)
                        {
                            rotAverageY += rotArrayY[j];
                        }
                        rotAverageY /= rotArrayY.Count;

                        rotAverageY = ClampAngle(rotAverageY, minimumY, maximumY);

                        Quaternion yQuaternion = Quaternion.AngleAxis(rotAverageY, Vector3.left);
                        transform.localRotation = originalRotation * yQuaternion;
                    }
                }
            }
        }
	}
	

	void Start ()
	{
        int[] settings = GameObject.FindWithTag("gsh").GetComponent<GameSparksHandler>().getSettings();
        sensitivityX = settings[3];
        sensitivityY = settings[4];

		Rigidbody rb = GetComponent<Rigidbody>();	
		if (rb)
			rb.freezeRotation = true;
		originalRotation = transform.localRotation;
        //hasPlayer = false;
        isInteracting = false;
        foundPlayer = false;
        gunDist = interactLength;
        cam = GetComponent<Camera>();
        camWidth = cam.pixelWidth;
        camHeight = cam.pixelHeight;
        crossHairScreenPos = new Vector3(camWidth / 2f, camHeight / 2f, cam.nearClipPlane);
        gameOver = false;
       
	}

	public static float ClampAngle (float angle, float min, float max)
	{
		angle = angle % 360;
		if ((angle >= -360F) && (angle <= 360F)) {
			if (angle < -360F) {
				angle += 360F;
			}
			if (angle > 360F) {
				angle -= 360F;
			}			
		}
		return Mathf.Clamp (angle, min, max);
	}

	public void setTheParent(Transform newParent) {
        
		//cameraPos.y = 8;
		//cameraPos.x = newParent.forward.x + 3;
		//cameraPos.z = newParent.forward.z * -10;

   
		transform.SetPositionAndRotation (cameraPos, newParent.rotation);

        transform.Rotate(0, -30, 0);//
		originalRotation = transform.localRotation;
		parent = newParent.gameObject;
		transform.SetParent(newParent);

        //transform.localPosition = new Vector3(1.5f, 2f, -1f);//
        transform.localPosition = new Vector3(1.5f, 2f, -1f);

		pc = parent.GetComponent<playerController> ();
        pbc = parent.GetComponent<playerBlasterController>();
        gc = parent.GetComponentInChildren<GameController>();
		hasPlayer = true;
        pregameCanvas.SetActive(false);
	}

    public void setGunDistance(float distance){
        gunDist = distance;
    }
    //the first kind of shooting where an effect is instantiated on a spot used for BigBabyBlaster
   // public void shoot1(GameObject effect, int damage){
    public void shoot1(string effect, int damage)
    {  
        RaycastHit worldHit;
        //if something is hit
        if (Physics.Raycast(crossHairWorldPos, transform.TransformDirection(Vector3.forward), out worldHit, gunDist))
        {
            PhotonNetwork.Instantiate(effect, worldHit.point, Quaternion.identity,0);
            //Debug.Log("effect location: " + effect.transform.position);
           // Debug.Log("point location: " + worldHit.point);
            if(worldHit.collider.tag.Equals("Player") || worldHit.collider.tag.Equals("AI")){
                bool isRed;
                isRed = PhotonNetwork.LocalPlayer.GetTeam().Equals(PunTeams.Team.red);
                worldHit.collider.gameObject.GetComponent<PhotonView>().RPC("applyDamage", RpcTarget.AllBufferedViaServer, damage, parent.GetComponent<PhotonView>().ViewID, isRed);
            }
        }
        //spawns the effect at the point in the world
        else{
            PhotonNetwork.Instantiate(effect, transform.position + transform.forward * gunDist, Quaternion.identity,0);

        }
    }
    //the second kind of shooting where an effect is shot 
    //public void shoot2(GameObject effect, int damage, Vector3 gunTip) {
    public void shoot2(string effect, int damage, Vector3 gunTip)
    {
        RaycastHit worldHit;
        GameObject temp = PhotonNetwork.Instantiate(effect, gunTip, transform.rotation, 0);
        //hit something?
        if (Physics.Raycast(crossHairWorldPos, transform.TransformDirection(Vector3.forward), out worldHit, gunDist))
        {
            //Debug.Log("moving to " + worldHit.point);
            //temp.GetComponent<moveToPoint>().move(worldHit.point);

            if (worldHit.collider.tag.Equals("Player") || worldHit.collider.tag.Equals("AI"))
            {
                //worldHit.collider.gameObject.GetComponent<playerController>().loseHealth(damage);
                bool isRed;
                isRed = PhotonNetwork.LocalPlayer.GetTeam().Equals(PunTeams.Team.red);
                worldHit.collider.gameObject.GetComponent<PhotonView>().RPC("applyDamage",
                                                                            RpcTarget.AllBufferedViaServer,
                                                                            damage, 
                                                                            parent.GetComponent<PhotonView>().ViewID,
                                                                            isRed);
            }
        }
        //hit nothing
        else
        {
            //temp.GetComponent<moveToPoint>().move(transform.position + transform.forward * gunDist);
           // effect.GetComponent<moveToPoint>().move()
            // Debug.Log("point location: " + worldHit.point);
        }

    }

    public void shoot3(string projectile, Vector3 gunTip){
        GameObject proj = PhotonNetwork.Instantiate(projectile, pc.getProjectileOrigin(), transform.rotation, 0);
        proj.GetComponent<waterBalloonProjectile>().startMoving(transform.forward,parent.GetComponent<PhotonView>().ViewID,
                                                                PhotonNetwork.LocalPlayer.GetTeam().Equals(PunTeams.Team.red));
        Debug.Log("inatantiating bullet at: " + gunTip.ToString());
    }

    public void endGame(){
        gameOver = true;
    }

    //used to enact sensitivity changes in the code
    public void setAgainSettings(int x, int y){
        sensitivityX = (float) x;
        sensitivityY = (float)y;
    }
}