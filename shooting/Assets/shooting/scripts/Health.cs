﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System;

public class Health : NetworkBehaviour 
{
	public const int maxHealth = 100;
	[SyncVar(hook = "OnChangeHealth")] public int currentHealth = maxHealth;
	public RectTransform healthbar;
	public Text msg;
	public AudioClip deadSound;
	private AudioSource source;

	private TcpClient socketConnection;
	void Start()
	{
		msg.text = "";
		source = GetComponent<AudioSource>();
	}


	public void TakeDamage(int amount)
	{
		//if(!isServer) return;

	    currentHealth -= amount;
	    if (currentHealth <= 0)
	    {
			currentHealth = 0;
			gameObject.transform.Find("fire").gameObject.SetActive(true);
			
			if(isLocalPlayer)
			{
				source.PlayOneShot(deadSound, 1.0f);
				msg.text = "You Died!";
			}


			//send message to simulator to land drones
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
					string clientMessage ;
					if(isServer)
						clientMessage = "Player1 died";
					else
						clientMessage = "Player2 died";
					// Convert string message to byte array.                 
					byte[] clientMessageAsByteArray = Encoding.UTF8.GetBytes(clientMessage);
					// Write byte array to socketConnection stream.                 
					stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);
					Debug.Log("Client sent his message - should be received by server");
				}
			}
			catch (SocketException socketException)
			{
				Debug.Log("Socket exception: " + socketException);
			}
	    }
	    
	}

	void OnChangeHealth(int health)
	{
		healthbar.sizeDelta = new Vector2(health*2, healthbar.sizeDelta.y);
		currentHealth = health;
	}

}
