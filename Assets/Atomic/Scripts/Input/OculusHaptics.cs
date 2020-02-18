using System.Collections;
using UnityEngine;

public enum VibrationForce
{
    Light,
    Medium,
    Hard,
}


public class OculusHaptics : MonoBehaviour
{

    [SerializeField]
    OVRInput.Controller controllerMask;

    private OVRHapticsClip clipLightRight;
    private OVRHapticsClip clipMediumRight;
    private OVRHapticsClip clipHardRight;
    private OVRHapticsClip clipLightLeft;
    private OVRHapticsClip clipMediumLeft;
    private OVRHapticsClip clipHardLeft;


    public float lowViveHaptics { get; private set; }
    public float mediumViveHaptics { get; private set; }
    public float hardViveHaptics { get; private set; }


    private void Start()
    {
        InitializeOVRHaptics();
    }

    private void InitializeOVRHaptics()
    {

        int cnt = 10;
        clipLightRight = new OVRHapticsClip(cnt);
        clipMediumRight = new OVRHapticsClip(cnt);
        clipHardRight = new OVRHapticsClip(cnt);
        clipLightLeft = new OVRHapticsClip(cnt);
        clipMediumLeft = new OVRHapticsClip(cnt);
        clipHardLeft = new OVRHapticsClip(cnt);

        for (int i = 0; i < cnt; i++)
        {
            clipLightRight.Samples[i] = i % 2 == 0 ? (byte)0 : (byte)45;
            clipMediumRight.Samples[i] = i % 2 == 0 ? (byte)0 : (byte)100;
            clipHardRight.Samples[i] = i % 2 == 0 ? (byte)0 : (byte)180;
            clipLightLeft.Samples[i] = i % 2 == 0 ? (byte)0 : (byte)45;
            clipMediumLeft.Samples[i] = i % 2 == 0 ? (byte)0 : (byte)100;
            clipHardLeft.Samples[i] = i % 2 == 0 ? (byte)0 : (byte)180;

        }

        clipLightRight = new OVRHapticsClip(clipLightRight.Samples, clipLightRight.Samples.Length);
        clipMediumRight = new OVRHapticsClip(clipMediumRight.Samples, clipMediumRight.Samples.Length);
        clipHardRight = new OVRHapticsClip(clipHardRight.Samples, clipHardRight.Samples.Length);
        clipLightLeft = new OVRHapticsClip(clipLightLeft.Samples, clipLightLeft.Samples.Length);
        clipMediumLeft = new OVRHapticsClip(clipMediumLeft.Samples, clipMediumLeft.Samples.Length);
        clipHardLeft = new OVRHapticsClip(clipHardLeft.Samples, clipHardLeft.Samples.Length);
    }


    void OnEnable()
    {
        InitializeOVRHaptics();
    }

    public void VibrateLeft(VibrationForce vibrationForce)
    {
        var channel = OVRHaptics.LeftChannel;

        switch (vibrationForce)
        {
            case VibrationForce.Light:
                channel.Preempt(clipLightLeft);
                break;
            case VibrationForce.Medium:
                channel.Preempt(clipMediumLeft);
                break;
            case VibrationForce.Hard:
                channel.Preempt(clipHardLeft);
                break;
        }
    }

    public void VibrateRight(VibrationForce vibrationForce)
    {
        var channel = OVRHaptics.RightChannel;

        switch (vibrationForce)
        {
            case VibrationForce.Light:
                channel.Preempt(clipLightRight);
                break;
            case VibrationForce.Medium:
                channel.Preempt(clipMediumRight);
                break;
            case VibrationForce.Hard:
                channel.Preempt(clipHardRight);
                break;
        }
    }

    //public IEnumerator VibrateTime(VibrationForce force, float time)
    //{
    //    var channel = OVRHaptics.RightChannel;
    //    if (controllerMask == OVRInput.Controller.LTouch)
    //        channel = OVRHaptics.LeftChannel;

    //    for (float t = 0; t <= time; t += Time.deltaTime)
    //    {
    //        switch (force)
    //        {
    //            case VibrationForce.Light:
    //                channel.Queue(clipLight);
    //                break;
    //            case VibrationForce.Medium:
    //                channel.Queue(clipMedium);
    //                break;
    //            case VibrationForce.Hard:
    //                channel.Queue(clipHard);
    //                break;
    //        }
    //    }
    //    yield return new WaitForSeconds(time);
    //    channel.Clear();
    //    yield return null;

    //}
}