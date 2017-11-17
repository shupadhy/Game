﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// For type IntPtr.
using System;
// For DllImport.
using System.Runtime.InteropServices;

using System.Text;
public class Time {
    uint nsec;
    uint sec;
}

public class Header {
        uint seq;
	Time time;        
        string frame_id;
}

public class Position {
    double x, y, z;
}
public class Orientation{
    double x, y, w, z;
} 

public class PoseStamped
{
    Header header;
    Position position;
    Orientation orientation;
}

public class OculusRiftTouchController : MonoBehaviour {
    [DllImport("ROSClient.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr _ROSClient_init();

    [DllImport("ROSClient.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern void _ROSClient_initPublisher(IntPtr client);

    [DllImport("ROSClient.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern void _ROSClient_publish(IntPtr client, int[] buttons, int buttons_length, float[] axes, int axes_length);

    [DllImport("ROSClient.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern void _ROSClient_initSubscriber(IntPtr client);

    [DllImport("ROSClient.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool _ROSClient_isMsgAvailable(IntPtr client);

    [DllImport("ROSClient.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr _ROSClient_getMsg(IntPtr client);

    [DllImport("ROSClient.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern void _ROSClient_clearMsg(IntPtr client);

    IntPtr ROSClient;
	int[] buttons;
	float[] axes;
    string msg;	
	// Use this for initialization
	void Start () {
		ROSClient = _ROSClient_init();
		_ROSClient_initPublisher(ROSClient);//, new StringBuilder("hello"));
		_ROSClient_initSubscriber(ROSClient);
		buttons = new int[11];
		axes = new float[8];
	}
    
	// Update is called once per frame
	void Update () {
		OVRInput.Update(); // Has to be called at the beginning to interact with OVRInput.
        
        // Retrieve buttons status
		buttons[0] = OVRInput.Get(OVRInput.Button.One) ? 1 : 0;
		buttons[1] = OVRInput.Get(OVRInput.Button.Two) ? 1 : 0;
		buttons[2] = OVRInput.Get(OVRInput.Button.Three) ? 1 : 0;
		buttons[3] = OVRInput.Get(OVRInput.Button.Four) ? 1 : 0;
		buttons[9] = OVRInput.Get(OVRInput.Button.PrimaryThumbstick) ? 1 : 0;
		buttons[10] = OVRInput.Get(OVRInput.Button.SecondaryThumbstick) ? 1 : 0;
        
        // Retrieve axes status
		Vector2 axisStickLeft = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
		axes[0] = axisStickLeft.x;
		axes[1] = axisStickLeft.y;

		axes[2] = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger);		

		Vector2 axisStickRight = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
		axes[3] = axisStickRight.x;
		axes[4] = axisStickRight.y;
		
		axes[5] = OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger);
        
        // Send buttons and axes to ROS.	
		_ROSClient_publish(ROSClient, buttons, 11, axes, 8);
        
        // Read position from ROS.	
		if (_ROSClient_isMsgAvailable(ROSClient))
		{
		    msg = Marshal.PtrToStringAnsi(_ROSClient_getMsg(ROSClient));
		    _ROSClient_clearMsg(ROSClient);
		    Debug.Log(msg); 
		}
    }
}
