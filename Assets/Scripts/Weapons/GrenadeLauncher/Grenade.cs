using UnityEngine;

public class Grenade : Projectile, IIdentifiable
{
  public IdentifierData GetIdentifierData()
    {
        return new IdentifierData
        {
            color = Color.red,
            topText = "GRENADE!",
            bottomText = "BOOM!"
        };
    }
}
