using System.Collections.Generic;
using UnityEngine;

/// <summary>Lanca cada Renderer segmentado do modelo uma vez quando o inimigo morre.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Projeto PSX/Enemy Body Part Death Particles")]
public sealed class EnemyBodyPartDeathParticles : MonoBehaviour
{
    [Tooltip("Perfil exclusivo que define impulso, gravidade, massa e duracao das partes. Sem perfil, o componente nao emite particulas.")]
    [SerializeField] private EnemyBodyPartProfile profile;

    private readonly List<Renderer> bodyPartRenderers = new();

    public void EmitDeathParts(Vector3 attackDirection, int lethalDamage)
    {
        if (profile == null)
            return;

        bodyPartRenderers.Clear();
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (renderer is SkinnedMeshRenderer skinned &&
                skinned.sharedMesh != null)
            {
                bodyPartRenderers.Add(renderer);
            }
            else if (renderer is MeshRenderer meshRenderer &&
                meshRenderer.GetComponent<MeshFilter>()?.sharedMesh != null)
            {
                bodyPartRenderers.Add(renderer);
            }
        }

        Vector3 direction = attackDirection.sqrMagnitude > .0001f
            ? attackDirection.normalized
            : transform.forward;

        foreach (Renderer source in bodyPartRenderers)
        {
            if (!TryCreateDetachedMesh(source, out Mesh mesh,
                out bool ownsMesh, out Vector3 worldScale))
            {
                continue;
            }

            Vector3 scatter = Random.insideUnitSphere * profile.spawnScatterRadius;
            Vector3 velocity = direction * (profile.launchSpeed +
                lethalDamage * profile.speedPerDamage) +
                Vector3.up * profile.upwardSpeed +
                Random.insideUnitSphere * profile.randomSpeed;

            CombatParticleSystem.Instance.EmitBodyPart(mesh, ownsMesh,
                source.transform.position + scatter,
                source.transform.rotation, worldScale, velocity,
                source.sharedMaterials, profile);
        }
    }

    private static bool TryCreateDetachedMesh(Renderer source, out Mesh mesh,
        out bool ownsMesh, out Vector3 worldScale)
    {
        if (source is SkinnedMeshRenderer skinned)
        {
            mesh = new Mesh { name = $"{source.name} Body Part Snapshot" };
            // Mantem a escala do Transform fora do snapshot. A conversao
            // abaixo aplica a matriz mundial completa exatamente uma vez.
            // Com false, BakeMesh ja incorpora essa escala e a matriz a
            // aplicaria novamente, deixando a peca maior que o modelo.
            skinned.BakeMesh(mesh, true);
            TransformMeshIntoParticleSpace(mesh, source.transform);
            ownsMesh = true;
            worldScale = Vector3.one;
            return true;
        }

        MeshFilter filter = source.GetComponent<MeshFilter>();
        if (filter == null || filter.sharedMesh == null)
        {
            mesh = null;
            ownsMesh = false;
            worldScale = Vector3.one;
            return false;
        }

        if (!filter.sharedMesh.isReadable)
        {
            mesh = filter.sharedMesh;
            ownsMesh = false;
            worldScale = source.transform.lossyScale;
            return true;
        }

        mesh = Object.Instantiate(filter.sharedMesh);
        mesh.name = $"{source.name} Body Part Snapshot";
        TransformMeshIntoParticleSpace(mesh, source.transform);
        ownsMesh = true;
        worldScale = Vector3.one;
        return true;
    }

    private static void TransformMeshIntoParticleSpace(Mesh mesh,
        Transform sourceTransform)
    {
        Vector3[] vertices = mesh.vertices;
        Matrix4x4 localToWorld = sourceTransform.localToWorldMatrix;
        Matrix4x4 normalToWorld = localToWorld.inverse.transpose;
        Quaternion worldToParticleRotation =
            Quaternion.Inverse(sourceTransform.rotation);
        Vector3 particlePosition = sourceTransform.position;

        for (int index = 0; index < vertices.Length; index++)
        {
            Vector3 worldPosition = localToWorld.MultiplyPoint3x4(vertices[index]);
            vertices[index] = worldToParticleRotation *
                (worldPosition - particlePosition);
        }

        mesh.vertices = vertices;
        Vector3[] normals = mesh.normals;
        if (normals.Length == vertices.Length)
        {
            for (int index = 0; index < normals.Length; index++)
            {
                Vector3 worldNormal = normalToWorld.MultiplyVector(normals[index]);
                normals[index] = (worldToParticleRotation * worldNormal).normalized;
            }
            mesh.normals = normals;
        }
        else
        {
            mesh.RecalculateNormals();
        }

        mesh.RecalculateBounds();
    }
}
