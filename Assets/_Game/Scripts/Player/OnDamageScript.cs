using UnityEngine;
using UnityEngine.UI;

public class OnDamageScript : MonoBehaviour
{
    [SerializeField] LifeSystem _lifeSystem;
    [SerializeField] Image _damageImage;

    bool damaged = false;
    private void Start()
    {
        _lifeSystem.OnDamage += OnDamage;
    }
    private void OnDestroy()
    {
        _lifeSystem.OnDamage -= OnDamage;
    }
    void OnDamage(int health, int newHealth)
    {
        damaged = true;
        Color dmg = _damageImage.color;
        dmg.a = 75.0f/255.0f;
        _damageImage.color = dmg;
    }

    private void Update()
    {
        if (_damageImage.color.a * 255 <= 0.1f && damaged)
        {
            Color dmg = _damageImage.color;
            dmg.a = 0;
            _damageImage.color = dmg;
            damaged = false;
        }
        if (damaged)
        {
            Color dmg = _damageImage.color;
            dmg.a = Mathf.Lerp(dmg.a,0, Time.deltaTime);
            _damageImage.color = dmg;
        }
    }
}
