using UnityEngine;

namespace ForestJourney
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class CollectionFeedback : MonoBehaviour
    {
        public CoinManager manager;
        public AudioClip chime;
        public Material sparkMaterial;
        [Min(1)] public int poolSize = 4;
        AudioSource source;
        ParticleSystem[] pool;
        int nextSpark;
        public int PooledEffectCount => pool == null ? 0 : pool.Length;

        void Awake()
        {
            source=GetComponent<AudioSource>();source.playOnAwake=false;source.volume=.28f;
            source.spatialBlend=0;source.ignoreListenerPause=true;
            pool=new ParticleSystem[Mathf.Max(1,poolSize)];
            for(int i=0;i<pool.Length;i++) pool[i]=CreateSpark(i);
        }
        void OnEnable() { if(manager?.coins!=null) foreach(var coin in manager.coins) if(coin) coin.PickedUp+=Celebrate; }
        void OnDisable() { if(manager?.coins!=null) foreach(var coin in manager.coins) if(coin)coin.PickedUp-=Celebrate; }
        void Celebrate(Vector3 position)
        {
            if(chime) {source.pitch=1+manager.Collected*.035f;source.PlayOneShot(chime);}
            var particles=pool[nextSpark];nextSpark=(nextSpark+1)%pool.Length;
            particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.transform.position=position;
            particles.Play();
        }
        ParticleSystem CreateSpark(int index)
        {
            var go=new GameObject("Coin collection sparkle " + (index+1));
            go.transform.SetParent(transform,false);
            var particles=go.AddComponent<ParticleSystem>();
            particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=particles.main;main.duration=.45f;main.loop=false;main.startLifetime=.65f;
            main.startSpeed=3.5f;main.startSize=.13f;main.startColor=new Color(1,.75f,.22f);
            main.gravityModifier=.6f;main.playOnAwake=false;main.useUnscaledTime=true;
            var emission=particles.emission;emission.rateOverTime=0;
            emission.SetBursts(new[]{new ParticleSystem.Burst(0,18)});
            var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.3f;
            particles.GetComponent<ParticleSystemRenderer>().sharedMaterial=sparkMaterial;
            return particles;
        }
    }
}
