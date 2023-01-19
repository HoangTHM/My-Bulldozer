using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Windows.Speech;
using TMPro;
public class ControlBull : MonoBehaviour
{
    [SerializeField] private WheelCollider WheelFrontLeftCollider;
    [SerializeField] private WheelCollider WheelFrontRightCollider;
    [SerializeField] private WheelCollider WheelRearLeftCollider;
    [SerializeField] private WheelCollider WheelRearRightCollider;

    [SerializeField] private Transform WheelFrontLeftTransform;
    [SerializeField] private Transform WheelFrontRightTransform;
    [SerializeField] private Transform WheelRearLeftTransform;
    [SerializeField] private Transform WheelRearRightTransform;
    
    /* Bulldozer moving*/
    private float verticalInput;
    private float horizontalInput;
    private float motorForce;
    private float motorForceMin = 300;
    private float motorForceMax = 1000;
    private float breakForce = 2000;
    private float maxSteeringAngle = 20;
    private bool isBreaking = false;
    private float currentBreakingforce = 0f;
    private float currentSteeringAngle = 0f;
    private float maxSpeed = 60; // km/h
    private float currentSpeed = 0;
    
    /* Control arm */
    [SerializeField] private Transform Arm;
    [SerializeField] private Transform Blade;
    private float ArmChange = 0;
    private float BladeChange = 0;
    private float ArmValue = 0;
    private float BladeValue = 0;
    private float ArmValueMax = 30;
    private float ArmValueMin = -90;
    private float BladeValueMax = 90;
    private float BladeValueMin = -45;

    /* control light*/
    [SerializeField] Light LightLeft;
    [SerializeField] Light LightRight;

    public TMP_Text SpeedText;

    /* Audio */
    public AudioSource soundRun;
    public AudioSource soundArm;
    private float minPitch = 0.8f;
    private float maxPitch = 1.5f;

    /* Wind */
    public WindZone windZone;
    private float windMax = 1.0f;

    /* Speech */
    private KeywordRecognizer keywordRecognizer;
    private Dictionary<string, Action> actions = new Dictionary<string, Action>();
    private bool isSwitchController = true;
    private void Start()
    {
        AudioSetup();
        LightSetup();
        WindZoneSeup();
        SpeechSetup();
    }
    private void Update() 
    {
        HandleLight();
        HandleAudio();
        HandleWindZone();
        
        SpeedText.text = "Speed: " + currentSpeed.ToString("f0") + " km/h";
    }
    private void FixedUpdate()
    {
        SwitchController();
        HandleMotor();
        HandleSteering();
        
        UpdateWheel();

        ChangeArmValue();
        HandleArm();
    }
    void SwitchController()
    {
        if(Input.GetKeyDown(KeyCode.Z))
        {
            isSwitchController = !isSwitchController;
        }
        if(isSwitchController)
        {
            GetInput();
            Debug.Log("Control by keyboard");
        }
        else
        {
            //SpeechSetup();
            Debug.Log("Control by speech");
        }
        

    }
    /* -------------------------------------- Control Move -------------------------------------- */
    void GetInput()
    {
        verticalInput = Input.GetAxis("Vertical");
        horizontalInput = Input.GetAxis("Horizontal");
        isBreaking = Input.GetKey(KeyCode.Space);
    }
    void HandleMotor()
    {
        float speedTemp = WheelRearLeftCollider.rpm < WheelRearRightCollider.rpm ? WheelRearLeftCollider.rpm : WheelRearRightCollider.rpm;
        currentSpeed = speedTemp * 2 * Mathf.PI * WheelFrontLeftCollider.radius * 60f / 1000f;

        motorForce = Mathf.Lerp(motorForceMin, motorForceMax, Mathf.Abs(currentSpeed)/maxSpeed);
        if(currentSpeed < maxSpeed && currentSpeed > -maxSpeed && !isBreaking)
        {
            WheelRearRightCollider.motorTorque = verticalInput * motorForce;
            WheelRearLeftCollider.motorTorque = verticalInput * motorForce;
            WheelFrontRightCollider.motorTorque = verticalInput * motorForce;
            WheelFrontLeftCollider.motorTorque = verticalInput * motorForce;            
        }
        else
        {
            WheelRearRightCollider.motorTorque = 0;
            WheelRearLeftCollider.motorTorque = 0;
            WheelFrontRightCollider.motorTorque = 0;
            WheelFrontLeftCollider.motorTorque = 0;   
        }
        currentBreakingforce = isBreaking ? breakForce : 0f;
        HandleBreaking();
    }
    void HandleBreaking()
    {
        WheelFrontRightCollider.brakeTorque = currentBreakingforce;
        WheelFrontLeftCollider.brakeTorque = currentBreakingforce;
        WheelRearRightCollider.brakeTorque = currentBreakingforce;
        WheelRearLeftCollider.brakeTorque = currentBreakingforce;
    }
    void HandleSteering()
    {
        currentSteeringAngle = horizontalInput * maxSteeringAngle;
        WheelFrontLeftCollider.steerAngle = currentSteeringAngle;
        WheelFrontRightCollider.steerAngle = currentSteeringAngle;
    }
    void UpdateWheel()
    {
        UpdateSingleWheel(WheelFrontLeftCollider, WheelFrontLeftTransform);
        UpdateSingleWheel(WheelFrontRightCollider, WheelFrontRightTransform);
        UpdateSingleWheel(WheelRearLeftCollider, WheelRearLeftTransform);
        UpdateSingleWheel(WheelRearRightCollider, WheelRearRightTransform);
    }
    void UpdateSingleWheel(WheelCollider col, Transform trans)
    {
        Vector3 position;
        Quaternion rotation;
        col.GetWorldPose(out position, out rotation);

        trans.position = position;
        trans.rotation = rotation;
    }

    /* -------------------------------------- Control Arm -------------------------------------- */
    void HandleArm()
    {
        ArmValue += ArmChange;
        Arm.localEulerAngles = new Vector3 (Mathf.Clamp(ArmValue * 10, ArmValueMin, ArmValueMax), Arm.localEulerAngles.y, Arm.localEulerAngles.z);

        BladeValue += BladeChange;
        Blade.localEulerAngles = new Vector3 (Mathf.Clamp(BladeValue * 10, BladeValueMin, BladeValueMax), Blade.localEulerAngles.y, Arm.localEulerAngles.z);
    }
    void ChangeArmValue()
    {
        /* Arm controller*/
        if (Input.GetKey(KeyCode.Keypad4) || Input.GetKey(KeyCode.Keypad7))
        {
            if (Input.GetKey(KeyCode.Keypad4))
            {
                ArmChange = Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.Keypad7))
            {
                ArmChange = -Time.deltaTime;
            }
        }
        else
        {
            ArmChange = 0;
        }

        /* Blade controller*/
        if (Input.GetKey(KeyCode.Keypad5) || Input.GetKey(KeyCode.Keypad8))
        {
            if (Input.GetKey(KeyCode.Keypad5))
            {
                BladeChange = Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.Keypad8))
            {
                BladeChange = -Time.deltaTime;
            }
        }
        else
        {
            BladeChange = 0;
        }
    }

    /* -------------------------------------- Control Light --------------------------------------*/
    void HandleLight()
    {
        if(Input.GetKeyDown(KeyCode.F))
        {
            LightLeft.enabled = !LightLeft.enabled;
            LightRight.enabled = !LightRight.enabled;
        }
    }
    void LightSetup()
    {
        /*Left Light*/
        LightLeft.type = LightType.Spot;
        LightLeft.range = 300;
        LightLeft.intensity = 1.5f;
        LightLeft.spotAngle = 60;

        /*Right Light*/
        LightRight.type = LightType.Spot;
        LightRight.range = 300;
        LightRight.intensity = 1.5f;
        LightRight.spotAngle = 60;
    }

    /* -------------------------------------- Control Audio -------------------------------------- */
    void HandleAudio()
    {
        soundRun.pitch = Mathf.Lerp(minPitch, maxPitch, Mathf.Abs(currentSpeed)/maxSpeed);
        soundRun.volume = Mathf.Lerp(0.5f, 1, Mathf.Abs(currentSpeed)/maxSpeed);

        if(ArmChange != 0 || BladeChange != 0)
        {
            soundArm.volume = 1;
        }
        else
        {
            soundArm.volume = 0;
        }
    }
    void AudioSetup()
    {
        /* Bulldozer */
        soundRun.volume = 0;
        soundRun.spatialBlend = 1;
        soundRun.rolloffMode = AudioRolloffMode.Linear;
        soundRun.maxDistance = 150;
        soundRun.loop = true;
        soundRun.Play();
        
        /* Arm */
        soundArm.volume = 0;
        soundArm.spatialBlend = 1;
        soundArm.rolloffMode = AudioRolloffMode.Linear;
        soundArm.maxDistance = 100;
        soundArm.loop = true;
        soundArm.Play();
    }

    /* Control Wind */
    void WindZoneSeup()
    {
        windZone.windMain = 0;
        windZone.windTurbulence = 0;
    }
    void HandleWindZone()
    {
        windZone.windMain = windZone.windTurbulence = Mathf.Lerp(0, windMax, Mathf.Abs(currentSpeed)/maxSpeed);
    }

    /* -------------------------------------- Control Speech -------------------------------------- */
    void SpeechSetup()
    {
        actions.Add("run", Run);
        actions.Add("back", Back);
        actions.Add("stop", Stop);
        actions.Add("left", Left);
        actions.Add("right", Right);
        actions.Add("straight", Straight);

        keywordRecognizer = new KeywordRecognizer(actions.Keys.ToArray());
        keywordRecognizer.OnPhraseRecognized += RecognizedSpeech;
        keywordRecognizer.Start();
    }
    private void RecognizedSpeech(PhraseRecognizedEventArgs speech)
    {
        Debug.Log(speech.text);
        actions[speech.text].Invoke();
    }

    void Run()
    {
        verticalInput = 1;
        isBreaking = false;
    }
    void Back()
    {
        verticalInput = -1;
        isBreaking = false;
    }
    void Stop()
    {
        verticalInput = 0;
        isBreaking = true;
    }
    void Left()
    {
        horizontalInput = -1;
    }
    void Right()
    {
        horizontalInput = 1;
    }
    void Straight()
    {
        horizontalInput = 0;
    }

}
