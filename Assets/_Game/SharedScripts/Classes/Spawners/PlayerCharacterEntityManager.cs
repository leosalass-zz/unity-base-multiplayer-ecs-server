using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using System.Collections.Generic;

public class PlayerCharacterEntityManager
{
    private EntityManager entityManager;
    private EntityArchetype playerCharacterArchetype;
    private World defaultWorld;
    private GameObjectConversionSettings settings;

    private Entity playerCharacterEntityPrefab;
    private Dictionary<int, Entity> _entities;

    public PlayerCharacterEntityManager()
    {
        _entities = new Dictionary<int, Entity>();
        defaultWorld = World.DefaultGameObjectInjectionWorld;
        entityManager = defaultWorld.EntityManager;
        settings = GameObjectConversionSettings.FromWorld(defaultWorld, null);
        SetPlayerCharacter_001();
    }

    public void SetPlayerCharacter_001()
    {
        GameObject prefab = Resources.Load("Characters/PlayerCharacters/Character_001/Prefabs/PlayerCharacter_001") as GameObject;
        playerCharacterEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, settings);
    }

    public void SpawnPlayerCharacterEntity(float3 pos, int peerId)
    {
        Entity entity = entityManager.Instantiate(playerCharacterEntityPrefab);
        entityManager.SetComponentData(entity, new Translation
        {
            Value = pos
        });
        entityManager.SetComponentData(entity, new PlayerCharacterConnectionComponent
        {

            id = peerId
        });
        entityManager.SetName(entity, "PlayercharacterEntity" + peerId);
        _entities.Add(peerId, entity);
    }

    public void DestroyEntity(int peerId)
    {
        entityManager.DestroyEntity(_entities[peerId]);
        _entities.Remove(peerId);
    }

    public void DestroyAllEntities()
    {
        foreach (KeyValuePair<int, Entity> entity in _entities)
        {
            entityManager.DestroyEntity(entity.Value);
        }

        _entities.Clear();
    }
}
