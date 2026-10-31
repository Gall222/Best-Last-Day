using Components;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    public partial struct CitizenDebugSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            int citizens = SystemAPI.QueryBuilder()
                .WithAll<Citizen>()
                .Build()
                .CalculateEntityCount();

            int movements = SystemAPI.QueryBuilder()
                .WithAll<Citizen, CitizenMovement>()
                .Build()
                .CalculateEntityCount();

            int states = SystemAPI.QueryBuilder()
                .WithAll<Citizen, CitizenStateComponent>()
                .Build()
                .CalculateEntityCount();

            int health = SystemAPI.QueryBuilder()
                .WithAll<Citizen, CitizenHealth>()
                .Build()
                .CalculateEntityCount();

            int transforms = SystemAPI.QueryBuilder()
                .WithAll<Citizen, LocalTransform>()
                .Build()
                .CalculateEntityCount();

            Debug.Log(
                $"Citizens: {citizens} | " +
                $"Movement: {movements} | " +
                $"States: {states} | " +
                $"Health: {health} | " +
                $"Transform: {transforms}"
            );

            if (citizens > 0)
                state.Enabled = false;
        }
    }
}