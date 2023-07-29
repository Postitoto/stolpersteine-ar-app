using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FPSDisplay : MonoBehaviour
{
    [SerializeField]
    private Text text;
	float deltaTime = 0.0f;
 
	void Update()
	{
		deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float msec = deltaTime * 1000.0f;
		float fps = 1.0f / deltaTime;
		string text_string = string.Format("{0:0.0} ms\n ({1:0.} fps)", msec, fps);
        text.text = text_string;
	}
 

}
