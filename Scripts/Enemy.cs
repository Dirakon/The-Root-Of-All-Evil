using System;
using Godot;

public partial class Enemy : CharacterBody2D
{
    private EnemyType _enemyType;

    public Vector2 AppliedKnockback = Vector2.Zero;
    [Export] public float AppliedToSelfKnockbackModifer = 10.0f;
    [Export] public float Damage = 10.0f;
    [Export] private string EnemyTypeId;
    [Export] public float Health = 10.0f;
    [Export] public float KnockbackStrength = 10.0f;
    [Export] public float knockBackToDampFactor;
    public Vector2 PersonalVelocity = Vector2.Zero;
    [Export] private Vector2 SelfScale;
    [Export] public float Speed = 300.0f, Acceleraion;

    public bool IsInfected()
    {
        return EnemyTypeId.ToLower().StartsWith("infected");
    }

    public override void _Ready()
    {
        Scale = SelfScale;
        _enemyType = EnemyTypeId.ToLower() switch
        {
            "melee" => new MeleeEnemy(),
            "infectedmelee" => new InfectedMeleeEnemy(),
            "infectedranged" => new InfectedRangedEnemy(),
            "ranged" => new RangedEnemy(),

            _ => throw new ArgumentOutOfRangeException()
        };
        _enemyType.Enemy = this;
    }

    public override void _PhysicsProcess(double delta)
    {
        LookAt(Hero.Instance().GlobalPosition);
        PersonalVelocity = _enemyType.ProcessPersonalVelocity(delta);


        Velocity = PersonalVelocity + AppliedKnockback * (float) delta;


        var knockbackDampingFactor = knockBackToDampFactor * delta;
        if (knockBackToDampFactor >= 1) knockbackDampingFactor = 1;

        AppliedKnockback *= 1 - (float) knockbackDampingFactor;

        var collision = MoveAndCollide(Velocity);
        if (collision?.GetCollider() == null)
            return;
        _enemyType.CollideWith(collision.GetCollider(), delta);
    }

    public void TakeDamage(float damage, Vector2 knockback)
    {
        Health -= damage;
        if (Health < 0)
            QueueFree();
        else
            AppliedKnockback += knockback * AppliedToSelfKnockbackModifer;
    }
}

public abstract class EnemyType
{
    public Enemy Enemy;

    public virtual void CollideWith(GodotObject collider, double delta)
    {
    }

    public virtual Vector2 ProcessPersonalVelocity(double delta)
    {
        return Enemy.GlobalTransform.X.Normalized() * (float) (delta * Enemy.Speed);
    }
}

public class MeleeEnemy : EnemyType
{
    public override void CollideWith(GodotObject collider, double delta)
    {
        var hero = collider as Hero;
        hero?.TakeDamage(delta * Enemy.Damage,
            Enemy.GlobalPosition.DirectionTo(hero.GlobalPosition) * Enemy.KnockbackStrength);
    }
}

public class RangedEnemy : EnemyType
{
    public override Vector2 ProcessPersonalVelocity(double delta)
    {
        return Vector2.Zero;
    }
}

public class InfectedRangedEnemy : EnemyType
{
    public override Vector2 ProcessPersonalVelocity(double delta)
    {
        return Vector2.Zero;
    }
}

public class InfectedMeleeEnemy : EnemyType
{
    private Vector2 currentPersonalVelocity = Vector2.Zero;

    public override void CollideWith(GodotObject collider, double delta)
    {
        switch (collider)
        {
            case Hero hero:
                hero.TakeDamage(delta * Enemy.Damage,
                    Enemy.GlobalPosition.DirectionTo(hero.GlobalPosition) * Enemy.KnockbackStrength);
                break;
            case null:
                break;
            case Node2D wall:
                if (!wall.Name.ToString().ToLower().StartsWith("wall")) return;
                currentPersonalVelocity = Vector2.Zero;
                Enemy.AppliedKnockback = Vector2.Zero;
                break;
        }
    }

    public override Vector2 ProcessPersonalVelocity(double delta)
    {
        currentPersonalVelocity += Enemy.GlobalTransform.X.Normalized() * (float) (delta * Enemy.Acceleraion);
        if (currentPersonalVelocity.Length() >= Enemy.Speed)
            currentPersonalVelocity = currentPersonalVelocity.Normalized() * Enemy.Speed;
        return currentPersonalVelocity;
    }
}