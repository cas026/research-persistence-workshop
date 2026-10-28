using System;

[Serializable]
public class PetData
{
    public string name;
    public int age;
    public float health;

    public override string ToString()
    {
        return "PetData: " + name + " (" + age + " years old, " + health + "hp)";
    }
}