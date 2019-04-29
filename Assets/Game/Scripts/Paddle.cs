﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Threading;
using System.Text;
using System;
using System.Net.Sockets;

public class Paddle : NetworkBehaviour
{

	//public bool isPaddle1;
	/*ROS Client*/
	private SceneConfiguration config;
	
	int[] buttons;
	//float[] axes;
	string msg;

	/*[Header("Config")]
	public bool useROS = false;
	public bool seeOpponent = true;
	public bool singlePlayer = false;
	[Range(0.0f, 1.0f)]
	public float videoDistance = 1.0f;
	//public GameObject gameObject = ;
	[Header("ROS")]
	public Vector3 positionScale;
	public string remoteIP = "192.168.1.116";
	public string publishingTopic = "joy";
	public string subscribingTopic = "fiducial_pose_corrected";
	public bool isSimulation = false; // ROS simulation.
	public bool enableRotation = false;
	public bool enableLowPassFilter = false;
	public int nTaps = 51;
	public double Fs = 44.1, Fx = 2.0;
	public bool enableMyFilter = false;
	public int bufferSize = 5;*/

	/*Game*/
	Vector3 positionOffset;
	Vector3 orientationOffset;
	/*[Header("Game")]
	public float speed = 1f;*/
	[Range(0.0f, 1.0f)]
	public float colliderDist = 0.1f;
	public GameObject ballprefab;
	[SyncVar]
	public bool ready = false;

	private Vector3 scale1;
	private Vector3 scale2;
	
	private ROS.Orientation ori = new ROS.Orientation();
	private ROS.Position pos = new ROS.Position();
	private ROS.Position wp_pos = new ROS.Position();
	private ROS.Orientation wp_ori = new ROS.Orientation();
	private Vector3 pose; //same as pose
	private Vector3 w_pose; //same as wp_pos
	private Vector3 movingDirection = new Vector3();


    /* NEW VERSION VARIABLES */

    private Thread clientReceiveThread;

    private TcpClient socketConnection;

    ROSClient rosClient;

    // All following variables are using Unity coord conventions

    private Vector3 current_position = new Vector3(0, 0, 0);

    private Vector3 current_orientation = new Vector3(0, 0, 0);

    private Vector3 target_displacement = new Vector3(0, 0, 0);

    private Vector3 target_position = new Vector3(0, 0, 0);

    private Vector3 target_orientation = new Vector3(0, 0, 0);


    private float[] controller_axes = new float[4];

    private Vector3 controller_displacement_threshold = new Vector3((float).5, (float).5, (float).5); // Action below this value will not be considered

    private Vector3 controller_displacement = new Vector3(0, 0, 0);

    private float controller_rotation = 0;

    private Vector3 moving_speed = new Vector3((float)1, (float).1, (float)1);

    private float rotation_speed = (float)10;


    [Command]
	void CmdSpawnBall()
	{
		Quaternion rot = new Quaternion();
		rot.eulerAngles = new Vector3(0.0f, 0.0f, 0.0f);
		Vector3 Pos = (GameObject.Find("StartPos1").GetComponent<Transform>().position + GameObject.Find("StartPos2").GetComponent<Transform>().position) / 2.0f;
		Vector3 ballPos = new Vector3(Pos.x, Pos.y + 0.2f, Pos.z);

		var ball = (GameObject)Instantiate(ballprefab, ballPos, rot);
		float sx = 0.0f;
		float sy = 0.0f;
		float sz = -1.0f;
		//float sx = Random.Range(-0.5f, 0.5f);
		//float sy = Random.Range(-0.5f, 0.5f);
		//float sz = Random.Range(-0.5f, 0.5f);
		ball.GetComponent<Rigidbody>().velocity = new Vector3(config.speed * sx, config.speed * sy, config.speed * sz);
		NetworkServer.Spawn(ball);
	}

	public override void OnStartClient()
	{
		config = GameObject.Find("Config").GetComponent<SceneConfiguration>();
		if (!isServer || config.singlePlayer)
		{
			Debug.Log("TEst");
			ready = true;
		}
	}

	void Start()
	{
        config = GameObject.Find("Config").GetComponent<SceneConfiguration>();
        if (!config.seeOpponent && !isLocalPlayer) gameObject.transform.Find("cockpit.785").gameObject.SetActive(false);
		if (!isLocalPlayer) return;
		/*adjust two minimaps*/
		var minimapp = gameObject.transform.Find("Plane").gameObject;
		scale1 = minimapp.transform.localScale;
		var minimapSide = gameObject.transform.Find("Plane2").gameObject;
		scale2 = minimapSide.transform.localScale;
		//float x = GameObject.Find("MinimapPlane").transform.localScale.x;
		//float z = GameObject.Find("MinimapPlane").transform.localScale.z;
		//minimapp.transform.localScale = new Vector3(scale1.x * z * 5.0f, scale1.y, scale1.z * x * 5.0f);

		//x = GameObject.Find("MinimapSidePlane").transform.localScale.x;
		//z = GameObject.Find("MinimapSidePlane").transform.localScale.z;
		//minimapSide.transform.localScale = new Vector3(scale1.x * z * 5.0f, scale1.y, scale1.z * x * 5.0f);


		float ratio = GameObject.Find("MinimapPlane").transform.localScale.x / GameObject.Find("MinimapPlane").transform.localScale.z;
		if (ratio < 1) minimapp.transform.localScale = new Vector3(scale1.x, scale1.y, scale1.z * ratio);
		else minimapp.transform.localScale = new Vector3(scale1.x / ratio, scale1.y, scale1.z);
		//Debug.Log(ratio);
		float ratio2 = GameObject.Find("MinimapSidePlane").transform.localScale.x / GameObject.Find("MinimapSidePlane").transform.localScale.z;
		if (ratio2 < 1) minimapSide.transform.localScale = new Vector3(scale2.x, scale2.y, scale2.z * ratio2);
		else minimapSide.transform.localScale = new Vector3(scale2.x / ratio2, scale2.y, scale2.z);
		//Debug.Log(ratio2);

		if (config.minimapOnPanel)
		{
			minimapp.transform.localPosition = new Vector3(-0.0581f, 0.1761f, 0.2448f);
			minimapSide.transform.localPosition = new Vector3(0.0648f, 0.1761f, 0.2448f);
			minimapp.transform.localEulerAngles = new Vector3(59.356f, 180.0f, 0.0f);
			minimapSide.transform.localEulerAngles = new Vector3(59.356f, 180.0f, 0.0f);
		}
		else
		{
			minimapp.transform.localPosition = new Vector3(0.0006f, 0.2793f, 0.215f);
			minimapp.transform.localEulerAngles = new Vector3(118.342f, 178.058f, -0.8109741f);
			minimapp.transform.localScale /= 1.2f;
			minimapSide.transform.localPosition = new Vector3(0.08f, 0.2625f, 0.1992f);
			minimapSide.transform.localEulerAngles = new Vector3(121.245f, 196.401f, -7.395996f);
			minimapSide.transform.localScale /= 1.2f;
		}

        // Hanqi's version

        if (config.useROS)

        {

            ConnectToTcpServer();



            //buttons = new int[4];

            //axes = new float[4];



            Debug.Log("Connecting to ROS master at " + config.remoteIP);

            rosClient = new ROSClient(config.remoteIP);

            if (config.enableMyFilter)

                rosClient.enableFilter(config.bufferSize);



            //Debug.Log("Connected");

            rosClient.initSubscriber(config.subscribingTopic);

            rosClient.initPublisher(config.publishingTopic);



        }
		/*read offset*/
		string offsetName;
		if (isServer) offsetName = "StartPos1";
		else offsetName = "StartPos2";
		positionOffset = GameObject.Find(offsetName).GetComponent<Transform>().position;
		orientationOffset = GameObject.Find(offsetName).GetComponent<Transform>().eulerAngles;
	}

    void UpdateControllerInfo()

    {

        /*

         * Controller

         * LThumbX: right - Unity: X

         * LThumbY: forward - Unity Z

         * RThumbX: rotate 

         * RThumbY: up - Unity Y

         * 

        */

        // Retrieve axes status  (LThumbstick Correct, RTsX is trigger, RTsY is RTsX) see Input Manager by edit->project settings->

        controller_axes[0] = Input.GetAxis("Oculus_GearVR_LThumbstickX");

        controller_axes[1] = Input.GetAxis("Oculus_GearVR_LThumbstickY");

        controller_axes[2] = Input.GetAxis("Oculus_GearVR_DpadX"); // fixed, invert unchecked

        controller_axes[3] = Input.GetAxis("Oculus_GearVR_RThumbstickY"); // invert checked



        // string[] axes_str = { controller_axes[0].ToString(), controller_axes[1].ToString(), controller_axes[2].ToString(), controller_axes[3].ToString() };

        // Debug.Log(string.Join(",", axes_str));



        controller_displacement.x = controller_axes[0];

        controller_displacement.y = controller_axes[2];

        controller_displacement.z = controller_axes[1];



        controller_rotation = controller_axes[3];



        // Keyboard Control

        // Direction: WASD

        // Rotation: QE

        // Up: LeftShift

        // Down: LeftCtrl

        if (Input.GetKey(KeyCode.W))

        {

            controller_displacement.z = 1;

        }

        if (Input.GetKey(KeyCode.S))

        {

            controller_displacement.z = -1;

        }

        if (Input.GetKey(KeyCode.A))

        {

            controller_displacement.x = -1;

        }

        if (Input.GetKey(KeyCode.D))

        {

            controller_displacement.x = 1;

        }

        if (Input.GetKey(KeyCode.E))

        {

            controller_rotation = 1;

        }

        if (Input.GetKey(KeyCode.Q))

        {

            controller_rotation = -1;

        }

        if (Input.GetKey(KeyCode.LeftShift))

        {

            controller_displacement.y = 1;

        }

        if (Input.GetKey(KeyCode.LeftControl))

        {

            controller_displacement.y = -1;

        }





        target_displacement.y = controller_displacement.y * moving_speed.y;



        // Unity: Left handed orientation system

        float theta = -transform.eulerAngles.y / 180 * Mathf.PI;



        float x = controller_displacement.x * moving_speed.x;

        float z = controller_displacement.z * moving_speed.z;



        target_displacement.x = x * Mathf.Cos(theta) - z * Mathf.Sin(theta);

        target_displacement.z = x * Mathf.Sin(theta) + z * Mathf.Cos(theta);

        target_orientation.y = current_orientation.y + controller_rotation * rotation_speed;

    }

    private void ConnectToTcpServer()

    {

        try

        {

            clientReceiveThread = new Thread(new ThreadStart(ListenForData));

            clientReceiveThread.IsBackground = true;

            clientReceiveThread.Start();

        }

        catch (Exception e)

        {

            Debug.Log("On client connect exception " + e);

        }

    }

	void MinimapControl()
	{
		Vector3 centerPos = gameObject.GetComponent<Transform>().position;
		Vector3 ballPos = new Vector3();
		if (GameObject.Find("Ball(Clone)") != null) ballPos = GameObject.Find("Ball(Clone)").GetComponent<Transform>().position;
		if (isServer)
		{
			GameObject.Find("MyDrone").GetComponent<Transform>().position = new Vector3(1.0f, centerPos.y / 3, centerPos.x / 3 - 100.0f);
			GameObject.Find("BallIndicator").GetComponent<Transform>().position = new Vector3(0.0f, ballPos.y / 3, ballPos.x / 3 - 100.0f);
			GameObject.Find("DroneSideView").GetComponent<Transform>().position = new Vector3(1.0f, centerPos.z / 3, centerPos.x / 3 - 200.0f);
			GameObject.Find("BallSideView").GetComponent<Transform>().position = new Vector3(0.0f, ballPos.z / 3, ballPos.x / 3 - 200.0f);

		}
		else
		{
			GameObject.Find("MyDrone").GetComponent<Transform>().position = new Vector3(1.0f, centerPos.y / 3, -centerPos.x / 3 - 100.0f);
			GameObject.Find("BallIndicator").GetComponent<Transform>().position = new Vector3(0.0f, ballPos.y / 3, -ballPos.x / 3 - 100.0f);
			GameObject.Find("DroneSideView").GetComponent<Transform>().position = new Vector3(1.0f, -centerPos.z / 3, -centerPos.x / 3 - 200.0f);
			GameObject.Find("BallSideView").GetComponent<Transform>().position = new Vector3(0.0f, -ballPos.z / 3, -ballPos.x / 3 - 200.0f);
		}



	}

	private Vector3 FindIntersection(Vector3 planeVector, Vector3 planePoint, Vector3 lineVector, Vector3 linePoint)
	{
		float vpt = lineVector.x * planeVector.x + lineVector.y * planeVector.y + lineVector.z * planeVector.z;
		float EPSILON = 0;
		if (System.Math.Abs(vpt) < EPSILON)
		{
			return new Vector3(9999, 9999, 9999);
		}

		else
		{
			/*Debug.Log("planePoint is " + planePoint);
            Debug.Log("linePoint is " + linePoint);
            Debug.Log("planeVector is " + planeVector);
            Debug.Log("lineVector is " + lineVector);*/
			float t = ((planePoint.x - linePoint.x) * planeVector.x + (planePoint.y - linePoint.y) * planeVector.y + (planePoint.z - linePoint.z) * planeVector.z) / vpt;
			return new Vector3(linePoint.x + lineVector.x * t, linePoint.y + lineVector.y * t, linePoint.z + lineVector.z * t);
		}

	}
	private bool CheckCollisionPoint(Vector3 collisionPoint)
	{
		float xMin = GameObject.Find("Wall5").GetComponent<Transform>().position.x;
		float xMax = GameObject.Find("Wall6").GetComponent<Transform>().position.x;


		float yMin = GameObject.Find("Wall").GetComponent<Transform>().position.y;
		float yMax = GameObject.Find("Wall2").GetComponent<Transform>().position.y;

		float zMin = GameObject.Find("Wall8").GetComponent<Transform>().position.z;
		float zMax = GameObject.Find("Wall7").GetComponent<Transform>().position.z;
		//Debug.Log("collision in checkCollision " + collisionPoint);
		if (collisionPoint.x > xMin - 0.1f && collisionPoint.x < xMax + 0.1f && collisionPoint.y > yMin - 0.1f && collisionPoint.y < yMax + 0.1f && collisionPoint.z > zMin - 0.1f && collisionPoint.z < zMax + 0.1f)
		{

			Vector3 directionToCollision = collisionPoint - current_position;
			movingDirection = target_displacement;
			//Debug.Log("direction is " + movingDirection);
			//Debug.Log("directionCollision is " + directionToCollision);
			/*if ((direction.x > 0 && directionToCollision.x < 0) || (direction.x < 0 && directionToCollision.x > 0))
                return false;
            if ((direction.y > 0 && directionToCollision.y < 0) || (direction.y < 0 && directionToCollision.y > 0))
                return false;
            if ((direction.z > 0 && directionToCollision.z < 0) || (direction.z < 0 && directionToCollision.z > 0))
                return false;*/
			if (movingDirection.x * directionToCollision.x < 0f || movingDirection.y * directionToCollision.y < 0f || movingDirection.z * directionToCollision.z < 0f)
				return false;
			return true;
		}

		return false;
	}

	

	public Vector3 GetFinalWayPoint(Vector3 collisionPoint, Vector3 origin)
	{
		Vector3 wayPoint = new Vector3();
		//Debug.Log("collisionPoint" + collisionPoint);
		if (collisionPoint.x > 0)
		{
			wayPoint.x = collisionPoint.x - 0.3f;
		}
		else
		{
			wayPoint.x = collisionPoint.x + 0.3f;
		}
		if (collisionPoint.y > 0)
		{
			wayPoint.y = collisionPoint.y - 0.3f;
		}
		else
		{
			wayPoint.y = collisionPoint.y + 0.3f;
		}
		if (collisionPoint.z > 0)
		{
			wayPoint.z = collisionPoint.z - 0.3f;
		}
		else
		{
			wayPoint.z = collisionPoint.z + 0.3f;
		}
		/*Debug.Log("in GetFinalWayPoint collison " + collisionPoint);
        

        Vector3 wayPoint = new Vector3();
        float sizeDrone = 0.1f;
        float test = origin.x - collisionPoint.x;
        Debug.Log("what is f " + test + " " + sizeDrone);
        // not enough distance between wall and origin point
        if (Math.Abs(origin.x - collisionPoint.x) <= sizeDrone)
        {
            Debug.Log("1 origin is " + origin);
            return origin;
        }
        else
        {
            if (collisionPoint.x < origin.x)
            {
                wayPoint.x = collisionPoint.x + sizeDrone;
            }
        }
        // determing the x and calculate y and z
        Vector3 direction = new Vector3(wayPoint.x - origin.x, wayPoint.y - origin.y, wayPoint.x - origin.y);
        float fraction = sizeDrone / direction.x;
        wayPoint.y = direction.y * fraction + collisionPoint.y;
        wayPoint.z = direction.z * fraction + collisionPoint.z;
        // make sure the final way point is between origin and collision point
        if (collisionPoint.y < origin.y && wayPoint.y > origin.y)
        {
            Debug.Log("2 origin is " + origin);
            return origin;
        }
            
        if (collisionPoint.z < origin.z && wayPoint.z > origin.z)
        {
            Debug.Log("3 origin is " + origin);
            return origin;
        }*/

		//if(collisionPoint.x < origin.x) {
		//    wayPoint.x = collisionPoint.x + 1.0f;
		//}
		//else {
		//    wayPoint.x = collisionPoint.x - 1.0f;
		//}
		//if(collisionPoint.y < origin.y) {
		//    wayPoint.y = collisionPoint.y + 1.0f;
		//}
		//else {
		//    wayPoint.y = collisionPoint.y - 1.0f;
		//}
		//if (collisionPoint.z < origin.z)
		//{
		//    wayPoint.z = collisionPoint.z + 1.0f;
		//}
		//else
		//{
		//    wayPoint.z = collisionPoint.z - 1.0f;
		//}
		// return wayPoint;
		return collisionPoint;
	}

	private int CalculateCollison()
	{
		//store calculated points in an array
		Vector3[] pointArray = new Vector3[6];

		pointArray[0] = FindIntersection(GameObject.Find("Wall2").GetComponent<Transform>().position - GameObject.Find("Wall").GetComponent<Transform>().position,
										 GameObject.Find("Wall").GetComponent<Transform>().position,
										 target_displacement, current_position);

		pointArray[1] = FindIntersection(GameObject.Find("Wall2").GetComponent<Transform>().position - GameObject.Find("Wall").GetComponent<Transform>().position,
										 GameObject.Find("Wall2").GetComponent<Transform>().position,
										target_displacement, current_position);

		pointArray[2] = FindIntersection(GameObject.Find("Wall8").GetComponent<Transform>().position - GameObject.Find("Wall7").GetComponent<Transform>().position,
										 GameObject.Find("Wall7").GetComponent<Transform>().position,
										 target_displacement, current_position);

		pointArray[3] = FindIntersection(GameObject.Find("Wall8").GetComponent<Transform>().position - GameObject.Find("Wall7").GetComponent<Transform>().position,
										 GameObject.Find("Wall8").GetComponent<Transform>().position,
										 target_displacement, current_position);

		pointArray[4] = FindIntersection(GameObject.Find("Wall6").GetComponent<Transform>().position - GameObject.Find("Wall5").GetComponent<Transform>().position,
										 GameObject.Find("Wall5").GetComponent<Transform>().position,
										 target_displacement, current_position);

		pointArray[5] = FindIntersection(GameObject.Find("Wall6").GetComponent<Transform>().position - GameObject.Find("Wall5").GetComponent<Transform>().position,
										 GameObject.Find("Wall6").GetComponent<Transform>().position,
										 target_displacement, current_position);


		//check which point is in the bounding box
		for (int i = 0; i < 6; i++)
		{
			if (pointArray[i].x >= 9997) continue;
			//Debug.Log("potential Point " + pointArray[i]);
			if (CheckCollisionPoint(pointArray[i]))
			{
				//Debug.Log("actual collision is " + pointArray[i]);
				//change global waypoint pos
				// w_pose = GetFinalWayPoint(pointArray[i], pose);
				// wp_pos.x = w_pose.x;
				// wp_pos.y = w_pose.z;
				// wp_pos.z = w_pose.y;

				//break;
				return i;
			}
		}
		return 7;

	}


	void CameraControl()
	{
		var mainCam = gameObject.transform.Find("Camera").gameObject;
		Vector3 camPos = new Vector3(mainCam.transform.position.x, mainCam.transform.position.y, mainCam.transform.position.z);
		Vector3 rot = new Vector3(mainCam.transform.eulerAngles.x, mainCam.transform.eulerAngles.y, mainCam.transform.eulerAngles.z);
		GameObject.Find("GoProPrefab").GetComponent<Transform>().position = camPos;
		GameObject.Find("GoProPrefab").GetComponent<Transform>().eulerAngles = rot;
	}


	void Update()
	{
		if (!isLocalPlayer) return;
		if (ready)
		{
			CmdSpawnBall();
			ready = false;
		}
		// transform.Translate(Input.GetAxis("Horizontal") * config.speed * Time.deltaTime, Input.GetAxis("Vertical") * config.speed * Time.deltaTime, 0f);
		
		//ROSControl();

		

		// Temporary not used
		if (config.dynamicAdjust)
		{
			var minimapp = gameObject.transform.Find("Plane").gameObject;
			//scale1 = minimapp.transform.localScale;
			var minimapSide = gameObject.transform.Find("Plane2").gameObject;
			//scale2 = minimapSide.transform.localScale;
			//float x = GameObject.Find("MinimapPlane").transform.localScale.x;
			//float z = GameObject.Find("MinimapPlane").transform.localScale.z;
			//minimapp.transform.localScale = new Vector3(scale1.x * z * 5.0f, scale1.y, scale1.z * x * 5.0f);

			//x = GameObject.Find("MinimapSidePlane").transform.localScale.x;
			//z = GameObject.Find("MinimapSidePlane").transform.localScale.z;
			//minimapSide.transform.localScale = new Vector3(scale1.x * z * 5.0f, scale1.y, scale1.z * x * 5.0f);


			float ratio = GameObject.Find("MinimapPlane").transform.localScale.x / GameObject.Find("MinimapPlane").transform.localScale.z;
			if (ratio < 1) minimapp.transform.localScale = new Vector3(scale1.x, scale1.y, scale1.z * ratio);
			else minimapp.transform.localScale = new Vector3(scale1.x / ratio, scale1.y, scale1.z);
			//Debug.Log(ratio);
			float ratio2 = GameObject.Find("MinimapSidePlane").transform.localScale.x / GameObject.Find("MinimapSidePlane").transform.localScale.z;
			if (ratio2 < 1) minimapSide.transform.localScale = new Vector3(scale2.x, scale2.y, scale2.z * ratio2);
			else minimapSide.transform.localScale = new Vector3(scale2.x / ratio2, scale2.y, scale2.z);

			if (config.minimapOnPanel)
			{
				minimapp.transform.localPosition = new Vector3(-0.0581f, 0.1761f, 0.2448f);
				minimapSide.transform.localPosition = new Vector3(0.0648f, 0.1761f, 0.2448f);
				minimapp.transform.localEulerAngles = new Vector3(59.356f, 180.0f, 0.0f);
				minimapSide.transform.localEulerAngles = new Vector3(59.356f, 180.0f, 0.0f);
			}
			else
			{
				minimapp.transform.localPosition = new Vector3(0.0006f, 0.2793f, 0.215f);
				minimapp.transform.localEulerAngles = new Vector3(118.342f, 178.058f, -0.8109741f);
				minimapp.transform.localScale /= 1.2f;
				minimapSide.transform.localPosition = new Vector3(0.08f, 0.2625f, 0.1992f);
				minimapSide.transform.localEulerAngles = new Vector3(121.245f, 196.401f, -7.395996f);
				minimapSide.transform.localScale /= 1.2f;
			}
		}
		if (config.singlePlayer) //cannot work with useROS!!!
		{


			//Debug.Log("single Player!");
			TestController();

		}

        if (config.useROS)

        {

            transform.position = current_position;

            transform.eulerAngles = current_orientation;



            UpdateControllerInfo();

            FinalizeTarget();



            SendFinalTarget();

        }



        else //move use keyboard, play on unity

        {

            moving_speed = new Vector3((float).07, (float).05, (float).07);
			rotation_speed = 0.1f;


			current_position = transform.position;

            current_orientation = transform.eulerAngles;



            // This will calculate target displacement and orientation

            UpdateControllerInfo();



            // This will add displacement to current position so that we have the final target

            FinalizeTarget();



            transform.position = target_position;

            transform.eulerAngles = target_orientation;



           

        }

		CameraControl();
		MinimapControl();
		gameObject.GetComponent<BoxCollider>().center = new Vector3(0.0f, 0.0f, 3.0f * colliderDist);
	}

    private void ListenForData()

    {

        try

        {

            socketConnection = new TcpClient(config.remoteIP, 13579);

            Byte[] bytes = new Byte[1024];

            if (socketConnection.Connected)

                Debug.Log("TCP Server connected.");

            while (true)

            {

                // Get a stream object for reading              

                using (NetworkStream stream = socketConnection.GetStream())

                {

                    int length;

                    // Read incomming stream into byte arrary.                  

                    while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)

                    {

                        var incommingData = new byte[length];

                        Array.Copy(bytes, 0, incommingData, 0, length);

                        // Convert byte array to string message.                        

                        string serverMessage = Encoding.UTF8.GetString(incommingData);

                        string[] pose_str = serverMessage.Split(',');

                        float x = float.Parse(pose_str[0]);

                        float y = float.Parse(pose_str[1]);

                        float z = float.Parse(pose_str[2]);

                        Vector3 ros_current_position = new Vector3(x, y, z);



                        // Coord conversion from ROS to Unity should have been done here.

                        current_position.x = ros_current_position.x;

                        current_position.y = ros_current_position.z;

                        current_position.z = ros_current_position.y;


						/*
                        float w = float.Parse(pose_str[3]);

                        x = float.Parse(pose_str[4]);

                        y = float.Parse(pose_str[5]);

                        z = float.Parse(pose_str[6]);

						*/

						//Vector3 ros_current_euler_orientation = (new Quaternion(x, y, z, w)).eulerAngles;
						Vector3 ros_current_euler_orientation = new Vector3();
						ros_current_euler_orientation.x = float.Parse(pose_str[3]);
						ros_current_euler_orientation.y = float.Parse(pose_str[4]);
						ros_current_euler_orientation.z = float.Parse(pose_str[5]);

						// 

						// From ROS_PoseStamped.cs euler.y, -euler.z  , -euler.x
						/*
						current_orientation.x = ros_current_euler_orientation.y;

                        current_orientation.y = -ros_current_euler_orientation.z;

                        current_orientation.z = -ros_current_euler_orientation.x;
						*/

						// OPTITRACK

						current_orientation.x = -ros_current_euler_orientation.z;

						current_orientation.y = -ros_current_euler_orientation.y;

						current_orientation.z = -ros_current_euler_orientation.x;

						Debug.Log(ros_current_euler_orientation);

                        //Debug.Log("server message received as: " + pose_str[0] + ',' + pose_str[1] + ',' + pose_str[2]);

                        //Debug.Log("server message received as: " + pose_str[3] + ',' + pose_str[4] + ',' + pose_str[5] + ',' + pose_str[6]);

                    }

                }

            }

        }

        catch (SocketException socketException)

        {

            Debug.Log("Socket exception: " + socketException);

        }

    }

    private void SendFinalTarget()

    {

        if (socketConnection == null)

        {

            return;

        }

        try

        {

            // Get a stream object for writing.             

            NetworkStream stream = socketConnection.GetStream();

            if (stream.CanWrite)

            {

                // Coord conversion from Unity to ROS should have been done here.

                Vector3 ros_final_target_position = new Vector3();



                ros_final_target_position.x = target_position.x;

                ros_final_target_position.y = target_position.z;

                ros_final_target_position.z = target_position.y;



                Vector3 ros_final_target_euler_orientation = new Vector3();

				ros_final_target_euler_orientation.x = -target_orientation.z;
				ros_final_target_euler_orientation.y = -target_orientation.y;
				ros_final_target_euler_orientation.z = -target_orientation.x;

				/*

                ros_final_target_euler_orientation.x = -target_orientation.z;

                ros_final_target_euler_orientation.y = target_orientation.x;

                ros_final_target_euler_orientation.z = -target_orientation.y;



                Quaternion ros_final_target_quaternion_orientation = Quaternion.Euler(ros_final_target_euler_orientation);

				*/

				string[] msg_str_array = new string[6];

                msg_str_array[0] = ros_final_target_position.x.ToString("f5");

                msg_str_array[1] = ros_final_target_position.y.ToString("f5");

                msg_str_array[2] = ros_final_target_position.z.ToString("f5");

				msg_str_array[3] = ros_final_target_euler_orientation.x.ToString("f5");

				msg_str_array[4] = ros_final_target_euler_orientation.y.ToString("f5");

				msg_str_array[5] = ros_final_target_euler_orientation.z.ToString("f5");

				/*

				msg_str_array[3] = ros_final_target_quaternion_orientation.w.ToString("f5");

                msg_str_array[4] = ros_final_target_quaternion_orientation.x.ToString("f5");

                msg_str_array[5] = ros_final_target_quaternion_orientation.y.ToString("f5");

                msg_str_array[6] = ros_final_target_quaternion_orientation.z.ToString("f5");

				*/

				string msg_str = String.Join(",", msg_str_array) + ",";

                //Debug.Log(final_target_orientation);

                // Convert string message to byte array.                 

                byte[] clientMessageAsByteArray = Encoding.UTF8.GetBytes(msg_str);

                // Write byte array to socketConnection stream.                 

                stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);

				// Debug.Log(msg_str);

			}

        }

        catch (SocketException socketException)

        {

            Debug.Log("Socket exception: " + socketException);

        }

    }

	private void FinalizeTarget()

	{

		// TODO: COLLISION DETECTION HERE



		if (VerifyTarget() || !config.collisionDetection)

		{

			target_position.x = current_position.x + target_displacement.x;

			target_position.y = current_position.y + target_displacement.y;

			target_position.z = current_position.z + target_displacement.z;

		}



        // COLLISION DETECTION END



        if (Mathf.Abs(controller_displacement.x) < controller_displacement_threshold.x && Mathf.Abs(controller_displacement.z) < controller_displacement_threshold.z)

        {

            // Idle Drift Prevention

            // If don't have last target, stay at current position; Else, stay at last target

            if (target_position.x == 0)

                target_position.x = current_position.x;

            if (target_position.z == 0)

                target_position.z = current_position.z;

        }



        if (Mathf.Abs(controller_displacement.y) < controller_displacement_threshold.y)

            if (target_position.y == 0)

                target_position.y = current_position.y;

    }

    private bool VerifyTarget()

    {

        // Vector3 checkCollisionPoint = new Vector3(0.0f, 0.0f, 0.0f);
		Vector3 checkCollisionPoint = current_position + target_displacement;
        float target_displacement_length = target_displacement.magnitude;

        // Debug.Log("length is " + target_displacement_length);
		int wall = CalculateCollison();
		if(wall == 0 || wall == 1) {
            if(target_displacement.y < 0) {
                checkCollisionPoint.y -= 1;
            } else if (target_displacement.y > 0) {
                checkCollisionPoint.y += 1;
            }
		} else if (wall == 2 || wall == 3) {
            if(target_displacement.z < 0) {
                checkCollisionPoint.z -= 1;
            } else if (target_displacement.z > 0) {
                checkCollisionPoint.z += 1;
            }
		} else if (wall == 4 || wall == 5) {
            if(target_displacement.x < 0) {
                checkCollisionPoint.x -= 1;
            } else if (target_displacement.x > 0) {
                checkCollisionPoint.x += 1;
            }
		}
        
        Vector3 checkObstacleCollision = new Vector3(0.0f, 0.0f, 0.0f);
        checkObstacleCollision.x = (float)((target_displacement.x / target_displacement_length) + target_displacement.x) + current_position.x;

        checkObstacleCollision.y = (float)((target_displacement.y / target_displacement_length) + target_displacement.y) + current_position.y;

        checkObstacleCollision.z = (float)((target_displacement.z / target_displacement_length) + target_displacement.z) + current_position.z;

		// checkCollisionPoint.x = (float)((target_displacement.x / target_displacement_length) + target_displacement.x) + current_position.x;

		// checkCollisionPoint.y = (float)((target_displacement.y / target_displacement_length) + target_displacement.y) + current_position.y;

		// checkCollisionPoint.z = (float)((target_displacement.z / target_displacement_length) + target_displacement.z) + current_position.z;



		//return (CheckPointInsideBox(checkCollisionPoint) && CheckNotCollideObstacle(checkObstacleCollision));
		return CheckPointInsideBox(checkCollisionPoint);


	}

    private bool CheckNotCollideObstacle(Vector3 point) {
        //Vector3 obstacle = GameObject.Find("Obstacle").GetComponent<Transform>().position;
        ////Debug.Log("obstacle: " + obstacle);
        ////Debug.Log("point: " + point);
        //if(Mathf.Abs(obstacle.x - point.x) < 0.01f && Mathf.Abs(obstacle.y - point.y) < 0.01f && Mathf.Abs(obstacle.z - point.z) < 0.01f) {
        //    Debug.Log("same point!!!!!!!!1");
        //    return false;
        //}
        //Vector3 obstacleToPoint = point - obstacle;
        //Vector3 curPosToObstacle = current_position - obstacle;
        ////Debug.Log("obstacleToPoint: " + obstacleToPoint + "curPosToObstacle: " + curPosToObstacle);
        ////if(obstacleToPoint.x / curPosToObstacle.x == obstacleToPoint.y / curPosToObstacle.y && obstacleToPoint.x / curPosToObstacle.x == obstacleToPoint.z / curPosToObstacle.z && curPosToObstacle.x * obstacleToPoint.x > 0 && curPosToObstacle.y * obstacleToPoint.y > 0 && curPosToObstacle.z * obstacleToPoint.z > 0) {
        ////    return true;
        ////}
        //Debug.Log("angle!!!!!" + Vector3.Angle(obstacleToPoint, curPosToObstacle));
        //float angle = Vector3.Angle(obstacleToPoint, curPosToObstacle);
        //if (angle < 5.0f || (180 - angle) < 5.0f) {
        //    Debug.Log("inside if!!!!!!!!!" + obstacle + "point " + point + "current " + current_position);
        //    if(((obstacle.x <= point.x && obstacle.x >= current_position.x) || (obstacle.x >= point.x && obstacle.x <= current_position.x)) && ((obstacle.y <= point.y && obstacle.y >= current_position.y) || (obstacle.y >= point.y && obstacle.y <= current_position.y)) && ((obstacle.z <= point.z && obstacle.z >= current_position.z) || (obstacle.z >= point.z && obstacle.z <= current_position.z))) {
        //        Debug.Log("return false" + angle + "point " + point);
        //        return false;
        //    }
        //}
        //return true;
        Collider box = GameObject.Find("Obstacle").GetComponent<Collider>();
        if(box.bounds.Contains(point)) {
            return false;
        }
        return true;
        //return !(Vector3.Dot((point - current_position).normalized, (obstacle - point).normalized) < 0f && Vector3.Dot((current_position - point).normalized, (obstacle - current_position).normalized) < 0f);
    }
    private bool CheckPointInsideBox(Vector3 point)

    {

        float xMin = GameObject.Find("Wall5").GetComponent<Transform>().position.x;
        float xMax = GameObject.Find("Wall6").GetComponent<Transform>().position.x;


        float yMin = GameObject.Find("Wall").GetComponent<Transform>().position.y;
        float yMax = GameObject.Find("Wall2").GetComponent<Transform>().position.y;

        float zMin = GameObject.Find("Wall8").GetComponent<Transform>().position.z;
        float zMax = GameObject.Find("Wall7").GetComponent<Transform>().position.z;


        if (point.x > xMin && point.x < xMax && point.y > yMin && point.y < yMax && point.z > zMin && point.z < zMax)

        {

            return true;

        }



        return false;

    }


    private void TestController() //debug wp
	{

		moving_speed = new Vector3((float).07, (float).05, (float).07);



		current_position = transform.position;

		current_orientation = transform.eulerAngles;


		if (Input.GetKey(KeyCode.W))

        {

            controller_displacement.z = 1;

        }

        if (Input.GetKey(KeyCode.S))

        {

            controller_displacement.z = -1;

        }

        if (Input.GetKey(KeyCode.A))

        {

            controller_displacement.x = -1;

        }

        if (Input.GetKey(KeyCode.D))

        {

            controller_displacement.x = 1;

        }

        if (Input.GetKey(KeyCode.E))

        {

            controller_rotation = 1;

        }

        if (Input.GetKey(KeyCode.Q))

        {

            controller_rotation = -1;

        }

        if (Input.GetKey(KeyCode.LeftShift))

        {

            controller_displacement.y = 1;

        }

        if (Input.GetKey(KeyCode.LeftControl))

        {

            controller_displacement.y = -1;

        }





        target_displacement.y = controller_displacement.y * moving_speed.y;



        // Unity: Left handed orientation system

        float theta = -transform.eulerAngles.y / 180 * Mathf.PI;



        float x = controller_displacement.x * moving_speed.x;

        float z = controller_displacement.z * moving_speed.z;



        target_displacement.x = x * Mathf.Cos(theta) - z * Mathf.Sin(theta);

        target_displacement.z = x * Mathf.Sin(theta) + z * Mathf.Cos(theta);

        target_orientation.y = current_orientation.y + controller_rotation * rotation_speed;

		FinalizeTarget();

		transform.position = target_position;


	}

	void OnApplicationQuit()
	{
		if (socketConnection.Connected)
			socketConnection.Close();
	}
}

