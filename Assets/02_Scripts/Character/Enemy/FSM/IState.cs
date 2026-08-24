namespace DungeonMaster.Character.Enemy.FSM
{
    public interface IState
    {
        void OnEnter(Enemy enemy);
        void OnUpdate(Enemy enemy);
        void OnExit(Enemy enemy);
    }
}
