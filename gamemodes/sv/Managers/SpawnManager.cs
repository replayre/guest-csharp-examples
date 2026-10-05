public class SpawnManager
{
    public struct SpawnState
    {
        public float Danger;
        public double LastUsedTime;
    }

    private readonly types.arena.SpawnPoint[] _spawnPoints;

    private readonly SpawnState[] _states;

    public SpawnManager(List<types.arena.SpawnPoint> spawnPoints)
    {
        _spawnPoints = spawnPoints.ToArray();
        _states = new SpawnState[_spawnPoints.Length];
    }

    public types.arena.SpawnPoint GetSpawnPoint()
    {
        int bestIndex = -1;
        float bestScore = float.MaxValue;

        double now = TimingHelper.GetTimeSeconds();

        for (int i = 0; i < _spawnPoints.Length; i++)
        {
            float score = EvaluateSpawn(i, now);
            if (score < bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        if (bestIndex == -1)
        {
            Console.WriteLine("[SpawnManager] Failed to find a good spawnpoint");
            bestIndex = Random.Shared.Next(_spawnPoints.Length);
        }

        _states[bestIndex].LastUsedTime = now;

        return _spawnPoints[bestIndex];
    }

    public void Tick(List<re.ClientId> players)
    {
        UpdateDangerScore(players);
    }

    private void UpdateDangerScore(List<re.ClientId> players)
    {
        for (int i = 0; i < _states.Length; i++)
            _states[i].Danger = 0f;

        foreach (var player in players)
        {
            re.EntityId controlledEntity = re.cl.ObserverComponent.GetPrimaryObserverEntity(player);
            if (!controlledEntity.IsValid() || !re.ecs.TransformComponent.Exists(controlledEntity))
            {
                continue;
            }

            Vector3 playerPos = re.ecs.TransformComponent.GetPosition(controlledEntity);

            for (int i = 0; i < _spawnPoints.Length; i++)
            {
                float distSq = (_spawnPoints[i].Position - playerPos).LengthSquared();
                if (distSq < 0.001f)
                    distSq = 0.001f;

                // Inverse-square falloff (worse penalty for close distance)
                //
                float dangerContribution = 1f / distSq;

                _states[i].Danger += dangerContribution;
            }
        }
    }

    private float EvaluateSpawn(int index, double now)
    {
        const float cooldownTime = 3f;
        const float cooldownWeight = 5f;

        ref var state = ref _states[index];

        float cooldownPenalty = 0f;

        float timeSinceUse = (float)(now - state.LastUsedTime);
        if (timeSinceUse < cooldownTime)
        {
            float t = 1f - (timeSinceUse / cooldownTime);
            cooldownPenalty = t * cooldownWeight;
        }

        return state.Danger + cooldownPenalty + Random.Shared.NextSingle() * 0.01f;
    }
}
