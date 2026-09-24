using UnityEngine;

namespace ForestJourney
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class CollectionFeedback : MonoBehaviour
    {
        public CoinManager manager;
        public AudioClip chime;
        public Material sparkMaterial;
        AudioSource source;
        void Awake() { source=GetComponent<AudioSource>();source.playOnAwake=false;source.volume=.28f;source.spatialBlend=0; }
        void OnEnable() { foreach(var coin in manager.coins) coin.PickedUp+=Celebrate; }
        void OnDisable() { foreach(var coin in manager.coins) if(coin)coin.PickedUp-=Celebrate; }
        void Celebrate(Vector3 position)
        {
            if(chime) {source.pitch=1+manager.Collected*.035f;source.PlayOneShot(chime);}
            var go=new GameObject("Coin collection sparkle");go.transform.position=position;
            var particles=go.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=particles.main;main.duration=.45f;main.loop=false;main.startLifetime=.65f;main.startSpeed=3.5f;main.startSize=.13f;main.startColor=new Color(1,.75f,.22f);main.gravityModifier=.6f;
            var emission=particles.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,18)});
            var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.3f;
            var renderer=particles.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=sparkMaterial;
            particles.Play();Destroy(go,1.4f);
        }
    }
}
