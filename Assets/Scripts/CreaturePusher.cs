using UnityEngine;

public class CreaturePusher : MonoBehaviour {

    Transform top;

    public void Start(){
        top = this.gameObject.transform.GetChild(0).transform;
    }

    int interval = 240;
    int count = 0;
    public void FixedUpdate(){
        if(count >= interval){
            count = 0;
           PutCreatureOnSide();
        }
        count++;
    }

    public void PutCreatureOnSide(){
        top.position = new Vector3(top.position.x, 0.4f, top.position.z);
        top.rotation = Quaternion.Euler(new Vector3(Random.Range(-195, -165), Random.Range(-180, 180), 90));
    }

}