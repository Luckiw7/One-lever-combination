using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KModkit;
using System.Linq;

public class LeverModule : MonoBehaviour {

	public KMBombModule Module;
	public KMBombInfo BombInfo;
	public KMSelectable lever;

	private static int ModuleIdCounter = 1;
	private int ModuleId = 0;

	public Animator leverAnimator;

	public KMAudio KMAudio;
	public AudioSource audioDiTestEditor;

	public Renderer LED0;
	public Renderer LED1;
	public Renderer LED2;

	public Renderer[] Lights;

	public Material LED_on;

	public Material Light_on;
	public Material Light_off;

	public int[] correct_sequence;
	public int[] input_sequence = {0,0,0,0,0};
	public bool input_started = false;

	private bool ModuleSolved = false;

	int LEDstate = 0;

	bool LeverActive = false;
	int ActiveLights = 0;

	void Awake() {
		ModuleId = ModuleIdCounter++;
	}

	// Use this for initialization
	void Start () {
		SetLEDs();
		GetSolution();
		lever.OnInteract += LeverPulled;
	}

	
	
	void SetLEDs(){
		if (Random.Range(0,2) == 1){LEDstate += 1; LED0.material = LED_on;}
		if (Random.Range(0,2) == 1){LEDstate += 2; LED1.material = LED_on;}
		if (Random.Range(0,2) == 1){LEDstate += 4; LED2.material = LED_on;}
		Debug.LogFormat("[One-lever combination #{0}] LED state = {1}", ModuleId, LEDstate);
	}



//Lever handling

	void ResetInput(){
		for(int i = 0; i<5 ; ++i){input_sequence[i] = 0;}
		Debug.LogFormat("[One-lever combination #{0}: Input reset", ModuleId);
	}

	bool CheckEmpty(){
		bool IsSequenceEmpty = true;
		for(int i = 0; i<5 ; ++i){
			if(input_sequence[i] != 0){IsSequenceEmpty = false;}
		}
		return !IsSequenceEmpty && ActiveLights==0;
	}
	
	bool LeverPulled() {
		if(!ModuleSolved){
				#if UNITY_EDITOR
            
            		if (audioDiTestEditor != null) {
               			audioDiTestEditor.Play();
            		}
       		 	#else
            	KMAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
       		 	#endif

				LeverActive = !LeverActive;
				leverAnimator.SetBool("IsPulled",LeverActive);
				StopAllCoroutines();
				StartCoroutine(ChangeLights());
				InputNumber();
			}
		return false;
		}

	void InputNumber(){
		if(input_started){
					for(int i = 0; i<5 ; ++i){
						if(input_sequence[i]==0){
							input_sequence[i]=ActiveLights;
							break;
						}
					}
					Debug.LogFormat("[One-lever combination #{0}: Current input sequence = {1}{2}{3}{4}{5}",ModuleId, input_sequence[0], input_sequence[1], input_sequence[2], input_sequence[3], input_sequence[4], " (0 means empty)");
					if(CheckFull()){StartCoroutine(CheckSolution());}
				}
				else{input_started = true;}
	}

	IEnumerator ChangeLights(){
		if(!ModuleSolved){
		yield return new WaitForSeconds(0.8f);
		while(LeverActive){
			if(ActiveLights < Lights.Length){
			ActiveLights++;
			Lights[ActiveLights - 1].material = Light_on;
			}
	
			else{
				yield return new WaitForSeconds(1.0f);
				if(LeverActive){
				LeverActive = false;
				leverAnimator.SetBool("IsPulled",LeverActive);
				ActiveLights = 0;
				ResetInput();
				for(int i = 0; i<Lights.Length; i++){
					Lights[i].material = Light_off;
				}
				}
			}
			
			yield return new WaitForSeconds(0.8f);
		}
			while(!LeverActive && ActiveLights > 0){
			if(ActiveLights > 0){
			ActiveLights += -1;
			Lights[ActiveLights].material = Light_off;
			}

			while(CheckEmpty()){ResetInput();}
			yield return new WaitForSeconds(0.8f);

		}
	}
	}


	//Determining solution
	int limit(int n){
		if(n<1){n=1;}
		else if(n>6){n=6;}
		return n;
	}


	void GetSolution(){
		//Determining bomb parameters
		int batteries = BombInfo.GetBatteryCount();
		int ports = BombInfo.GetPortCount();
		int lit = BombInfo.GetOnIndicators().Count();
		int unlit = BombInfo.GetOffIndicators().Count();
		bool vowel = BombInfo.GetSerialNumber().Any(c => "AEIOUaeiou".Contains(c));
		bool serial = (BombInfo.GetSerialNumberNumbers().LastOrDefault() % 2 == 0);

		int[] solution = {0,0,0,0,0};

		//Step 1:
    	if(LEDstate == 0 || LEDstate == 7){solution[0] = 6;}
    	else if(LEDstate == 1 || LEDstate == 2 || LEDstate == 4){solution[0] = 4;}
    	else{solution[0] = 5;}
	
    	//Step 2:
    	if((LEDstate == 0 || LEDstate == 1 || LEDstate == 2 || LEDstate == 3)&&batteries >= 2){solution[1] = limit(solution[0] - batteries);}
    	else if ((LEDstate == 1 || LEDstate == 3 || LEDstate == 5 || LEDstate == 7) && ports >= 2){solution[1] = limit(solution[0] - ports);}
    	else{solution[1] = limit(solution[0]-3);}
    
		//Step 3:
    	if(!serial && lit >=1){solution[2] = limit(solution[1] + lit);}
    	else if(vowel && unlit >=2){solution[2] = limit(solution[1] + unlit);}
    	else{solution[2] = limit(solution[1] + 2);}
    
    	//Step 4:
    	if(solution[2] % 2 == 0){solution[3] = limit(solution[2] - 3);}
    	else if(solution[1] % 2 == 1){solution[3] = limit(solution[2] - 1);}
    	else{solution[3] = limit(solution[2] - 2);}
    
    	//Step 5:
    	if(LEDstate == 0){solution[4] = 5;}
    	else if(LEDstate == 2 || LEDstate == 5){solution[4] = 6;}
    	else{solution[4] = limit(solution[3]+solution[1]);}
    
    	Debug.LogFormat(" [One-lever combination #{0}]: solution: {1}{2}{3}{4}{5}", ModuleId, solution[0], solution[1], solution[2], solution[3], solution[4]);
    	correct_sequence = solution;
	}



	//Checking solution

	bool CheckFull(){
		bool IsSequenceFull = true;
		for(int i = 0; i<5 ; ++i){
			if(input_sequence[i] == 0){IsSequenceFull = false;}
		}
		return IsSequenceFull;
	}

	IEnumerator CheckSolution(){
		bool SequenceMatch = true;
		Debug.LogFormat("[One-lever combination #{0}]: Final input sequence = {1}{2}{3}{4}{5}", ModuleId, input_sequence[0], input_sequence[1], input_sequence[2], input_sequence[3], input_sequence[4]);
		for(int i = 0; i<5 ; ++i){
			if(input_sequence[i] != correct_sequence[i]){SequenceMatch = false;}
		}
		if(SequenceMatch){
			yield return new WaitForSeconds(0.5f);
			GetComponent<KMBombModule>().HandlePass();
			ModuleSolved = true;
			Debug.LogFormat("[One-lever combination #{0}] Correct input sequence: module solved!", ModuleId);
			StopAllCoroutines();
		}
		else{
			GetComponent<KMBombModule>().HandleStrike();
			LeverActive = false;
			leverAnimator.SetBool("IsPulled",LeverActive);
			ActiveLights = 0;
			ResetInput();
			yield return new WaitForSeconds(0.9f);
			for(int i = 0; i<Lights.Length; i++){
					Lights[i].material = Light_off;
				}
			Debug.LogFormat("[One-lever combination #{0}] Incorrect input sequence: strike", ModuleId);
			StopAllCoroutines();
		}
		yield return new WaitForSeconds(0.01f);
	}
}