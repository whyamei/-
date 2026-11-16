using UnityEngine;

 public class FollowCamera : MonoBehaviour
    {
       [Header("目标跟随")]
       public Transform target;

       [Header("相机位置")]
       public Vector3 offset = new Vector3(0f, 11f,-10f);
     
      [Header("跟随平滑度")]
      public float smooth = 8f;

     void LateUpdate()
    {
        if (target == null) { return; }
        Vector3 want=target.position+offset;
        transform.position = Vector3.Lerp(transform.position, want, smooth*Time.deltaTime);
        transform.LookAt(target.position+Vector3.up*0.8f);

    }
}
