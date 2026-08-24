namespace DungeonMaster.Character.Enemy.FSM
{
    public class StateMachine
    {
        private Enemy _enemy;

        // 현재 상태를 저장하는 변수
        protected IState _currState;

        // 생성자
        public StateMachine(Enemy enemy)
        {
            _enemy = enemy;
        }

        // 상태를 전환하는 메서드
        public void ChangeState(IState newState)
        {
            _currState?.OnExit(_enemy);
            _currState = newState;
            _currState?.OnEnter(_enemy);
        }

        // 상태를 업데이트 하는 메서드(유니티의 Update 함수 아님)
        public void Update()
        {
            _currState?.OnUpdate(_enemy);
        }
    }
}