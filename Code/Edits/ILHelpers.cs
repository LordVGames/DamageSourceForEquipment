using System;
using UnityEngine;
using RoR2;
using Mono.Cecil.Cil;
using MonoDetour;
using MonoDetour.Cil;
using MonoDetour.DetourTypes;
using MonoDetour.HookGen;
using MonoMod.Cil;
using RoR2.Projectile;
using System.Text;
using MonoDetour.Cil.Analysis;
namespace DamageSourceForEquipment.Edits;

internal static class ILHelpers
{
    internal static void LogILInstructions(this ILWeaver w)
    {
        foreach (Instruction instruction in w.Instructions)
        {
            Log.Warning(instruction);
        }
    }



    internal static ILWeaverResult MatchNextRelaxed(this ILWeaver w, params Predicate<Instruction>[] predicates)
    {
        bool foundNextMatch = false;
        int oldWeaverOffset = w.Current.Offset;
        Instruction instructionToStayOn = null;

        ILWeaverResult matchResult = w.MatchMultipleRelaxed(
            onMatch: w2 =>
            {
                //Log.Debug($"w.Current.Offset {w.Current.Offset}");
                //Log.Debug($"w.Current {w.Current}");
                //Log.Debug($"w2.Current {w2.Current}");
                if (w2.Current.Offset > oldWeaverOffset && !foundNextMatch)
                {
                    //Log.Debug("FOUND");
                    foundNextMatch = true;
                    instructionToStayOn = w2.Current;
                }
            },
            predicates
        );
        if (!foundNextMatch)
        {
            return new ILWeaverResult(w, w.GetMatchToNextRelaxedErrorMessage);
        }

        w.SetCurrentTo(instructionToStayOn); // idk, just in case
        return matchResult;
    }
    private static string GetMatchToNextRelaxedErrorMessage(this ILWeaver w)
    {
        // this is stupid (i think?)
        StringBuilder sb = new();
        sb.Append(w.Method.Body.CreateInformationalSnapshotJIT().ToStringWithAnnotations());
        sb.AppendFormat($"\nLast Weaver Position: {w.Current}");
        sb.AppendFormat($"\nPrevious: {w.Previous}");
        sb.AppendFormat($"\nNext: {w.Next}");
        sb.AppendLine("\n\n! MatchNextRelaxed FAILED !\nA match was found, but it was not further ahead than the weaver's position!");
        return sb.ToString();
    }




    internal static void OverrideDamageInfoDamageSource(DamageSource damageSource, ILManipulationInfo info)
    {
        ILWeaver w = new(info);

        ILWeaverResult firstMatch = w.MatchRelaxed(
            x => x.MatchStfld<DamageInfo>("damageType") && w.SetCurrentTo(x)
        );
        if (firstMatch.IsValid)
        {
            w.InsertBeforeCurrent(
                w.CreateDelegateCall((DamageTypeCombo damageTypeCombo) =>
                {
                    damageTypeCombo.damageSource = damageSource;
                    return damageTypeCombo;
                })
            );
        }
        else
        {
            w.MatchRelaxed(
                x => x.MatchNewobj<DamageInfo>(),
                x => x.MatchDup() && w.SetCurrentTo(x)
            ).ThrowIfFailure();
            w.InsertAfterCurrent(
                w.CreateDelegateCall((DamageInfo damageInfo) =>
                {
                    damageInfo.damageType.damageSource = damageSource;
                    return damageInfo;
                })
            );
        }
    }
    internal static void OverrideNextDamageInfoDamageSource(DamageSource damageSource, ILWeaver w)
    {
        ILWeaverResult firstMatch = w.MatchNextRelaxed(
            x => x.MatchStfld<DamageInfo>("damageType") && w.SetCurrentTo(x)
        );
        if (firstMatch.IsValid)
        {
            w.InsertBeforeCurrent(
                w.CreateDelegateCall((DamageTypeCombo damageTypeCombo) =>
                {
                    damageTypeCombo.damageSource = damageSource;
                    return damageTypeCombo;
                })
            );
        }
        else
        {
            w.MatchNextRelaxed(
                x => x.MatchNewobj<DamageInfo>(),
                x => x.MatchDup() && w.SetCurrentTo(x)
            ).ThrowIfFailure();
            w.InsertAfterCurrent(
                w.CreateDelegateCall((DamageInfo damageInfo) =>
                {
                    damageInfo.damageType.damageSource = damageSource;
                    return damageInfo;
                })
            );
        }
    }



    internal static class Projectiles
    {
        internal static void AddDamageTypeComboToFireProjectileInfo(DamageTypeCombo damageTypeCombo, ILManipulationInfo info)
        {
            ILWeaver w = new(info);

            w.MatchRelaxed(
                x => x.MatchCallOrCallvirt<ProjectileManager>("FireProjectile") && w.SetCurrentTo(x)
            ).ThrowIfFailure();
            w.InsertBeforeCurrent(
                w.CreateDelegateCall((FireProjectileInfo fireProjectileInfo) =>
                {
                    if (!fireProjectileInfo.damageTypeOverride.HasValue)
                    {
                        fireProjectileInfo.damageTypeOverride = new DamageTypeCombo?(damageTypeCombo);
                    }
                    else
                    {
                        // why do i gotta do it like this, this is dumb
                        DamageTypeCombo tempDamageTypeOverrideValue = fireProjectileInfo.damageTypeOverride.Value;
                        tempDamageTypeOverrideValue.damageSource = damageTypeCombo.damageSource;
                        fireProjectileInfo.damageTypeOverride = tempDamageTypeOverrideValue;
                    }

                    return fireProjectileInfo;
                })
            );
        }


        internal static void OverrideFireProjectileWithoutDamageType(DamageTypeCombo damageTypeCombo, ILManipulationInfo info)
        {
            ILWeaver w = new(info);

            w.MatchRelaxed(
                x => x.MatchCallOrCallvirt<ProjectileManager>("FireProjectileWithoutDamageType") && w.SetCurrentTo(x)
            ).ThrowIfFailure();
            w.InsertBeforeCurrent(
                w.CreateDelegateCall(() =>
                {
                    return new DamageTypeCombo?(damageTypeCombo);
                })
            );
            w.ReplaceCurrent(
                w.Create<ProjectileManager>(OpCodes.Callvirt, "FireProjectile")
            );

            //w.LogILInstructions();
        }


        internal static void OverrideNextFireProjectileWithoutDamageType(DamageTypeCombo damageTypeCombo, ILWeaver w)
        {
            w.MatchNextRelaxed(
                x => x.MatchCallvirt<ProjectileManager>("FireProjectileWithoutDamageType") && w.SetCurrentTo(x)
            ).ThrowIfFailure();
            w.InsertBeforeCurrent(
                w.CreateDelegateCall(() =>
                {
                    return new DamageTypeCombo?(damageTypeCombo);
                })
            );
            w.ReplaceCurrent(
                w.Create<ProjectileManager>(OpCodes.Callvirt, "FireProjectile")
            );

            //w.LogILInstructions();
        }



        internal static void ReplaceNullDamageSourceInFireProjectile(DamageSource damageSource, ILManipulationInfo info)
        {
            ILWeaver w = new(info);

            w.MatchRelaxed(
                x => x.MatchCallOrCallvirt<ProjectileManager>("FireProjectile") && w.SetCurrentTo(x)
            ).ThrowIfFailure();
            w.InsertBeforeCurrent(
                w.CreateDelegateCall((DamageTypeCombo damageTypeCombo) =>
                {
                    damageTypeCombo.damageSource = damageSource;
                    return damageTypeCombo;
                })
            );
        }
    }



    internal static class BlastAttacks
    {
        internal static void OverrideBlastAttackDamageSource(DamageSource damageSource, ILManipulationInfo info)
        {
            ILWeaver w = new(info);

            ILWeaverResult matchedFire = w.MatchRelaxed(
                x => x.MatchCallOrCallvirt<BlastAttack>("Fire") && w.SetCurrentTo(x)
            );
            if (!matchedFire.IsValid)
            {
                w.MatchRelaxed(
                    x => x.MatchNewobj<BlastAttack>(),
                    x => x.MatchStfld(out _) && w.SetCurrentTo(x)
                ).ThrowIfFailure();
            }

            w.InsertBeforeCurrent(
                w.CreateDelegateCall((BlastAttack blastAttack) =>
                {
                    blastAttack?.damageType.damageSource = damageSource;
                    return blastAttack;
                })
            );
        }


        internal static void OverrideNextBlastAttackDamageSource(DamageSource damageSource, ILWeaver w)
        {
            ILWeaverResult matchedFire = w.MatchNextRelaxed(
                x => x.MatchCallOrCallvirt<BlastAttack>("Fire") && w.SetCurrentTo(x)
            );
            if (!matchedFire.IsValid)
            {
                w.MatchNextRelaxed(
                    x => x.MatchNewobj<BlastAttack>(),
                    x => x.MatchStfld(out _) && w.SetCurrentTo(x)
                ).ThrowIfFailure();
            }

            w.InsertBeforeCurrent(
                w.CreateDelegateCall((BlastAttack blastAttack) =>
                {
                    blastAttack?.damageType.damageSource = damageSource;
                    return blastAttack;
                })
            );
        }
    }



    internal static class OverlapAttacks
    {
        internal static void AddDamageSourceToOverlapAttack(DamageSource damageSource, ILManipulationInfo info)
        {
            ILWeaver w = new(info);

            w.MatchRelaxed(
                x => x.MatchNewobj<OverlapAttack>() && w.SetCurrentTo(x)
            ).ThrowIfFailure();
            w.CurrentToNext();
            w.InsertBeforeCurrent(
                w.CreateDelegateCall((OverlapAttack overlapAttack) =>
                {
                    overlapAttack?.damageType.damageSource = damageSource;
                    return overlapAttack;
                })
            );
        }

        internal static void OverrideDamageTypeCombo(DamageSource damageSource, ILManipulationInfo info)
        {
            ILWeaver w = new(info);

            w.MatchNextRelaxed(
                x => x.MatchStfld<OverlapAttack>("damageType") && w.SetCurrentTo(x)
            ).ThrowIfFailure();
            w.InsertBeforeCurrent(
                w.CreateDelegateCall((DamageTypeCombo damageTypeCombo) =>
                {
                    damageTypeCombo.damageSource = damageSource;
                    return damageTypeCombo;
                })
            );
        }

        internal static void OverrideNextDamageTypeCombo(DamageSource damageSource, ILWeaver w)
        {
            w.MatchNextRelaxed(
                x => x.MatchStfld<OverlapAttack>("damageType") && w.SetCurrentTo(x)
            ).ThrowIfFailure();
            w.InsertBeforeCurrent(
                w.CreateDelegateCall((DamageTypeCombo damageTypeCombo) =>
                {
                    damageTypeCombo.damageSource = damageSource;
                    return damageTypeCombo;
                })
            );
        }
    }



    internal static class BulletAttacks
    {
        internal static void OverrideBulletAttackDamageSource(DamageSource damageSource, ILManipulationInfo info)
        {
            ILWeaver w = new(info);

            w.MatchRelaxed(
                x => x.MatchCallOrCallvirt<BulletAttack>("Fire") && w.SetCurrentTo(x)
            ).ThrowIfFailure();

            w.InsertBeforeCurrent(
                w.CreateDelegateCall((BulletAttack bulletAttack) =>
                {
                    bulletAttack?.damageType.damageSource = damageSource;
                    return bulletAttack;
                })
            );
        }

        internal static void OverrideNextBulletAttackDamageSource(DamageSource damageSource, ILWeaver w)
        {
            w.MatchNextRelaxed(
                x => x.MatchCallOrCallvirt<BulletAttack>("Fire") && w.SetCurrentTo(x)
            ).ThrowIfFailure();

            w.InsertBeforeCurrent(
                w.CreateDelegateCall((BulletAttack bulletAttack) =>
                {
                    bulletAttack?.damageType.damageSource = damageSource;
                    return bulletAttack;
                })
            );
        }
    }
}