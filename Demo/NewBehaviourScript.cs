using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;

public class NewBehaviourScript : MonoBehaviour {
	[SerializeField] private Image m_img5; 
	[SerializeField] private Text m_txt6; 
	[SerializeField] private Button m_btn1; 
	[SerializeField] private Toggle m_tog2; 
	[SerializeField] private InputField m_Ipt3; 
	private string Ipt3 = "";
	[SerializeField] private Slider m_Sid4; 
	private float Sid4 = 0;
	private void Awake()
	{
		m_btn1.onClick.AddListener(Onbtn1Clicked); 
		m_tog2.onValueChanged.AddListener(Ontog2ValueChanged); 
		m_Ipt3.onValueChanged.AddListener(OnIpt3ValueChanged); 
		m_Sid4.onValueChanged.AddListener(OnSid4ValueChanged); 
	}
	private void Onbtn1Clicked()
	{
	}
	private void Ontog2ValueChanged(bool arg)
	{
	}
	private void OnIpt3ValueChanged(string arg)
	{
		Ipt3 = arg;
	}
	private void OnSid4ValueChanged(float arg)
	{
		Sid4 = arg;
	}
}