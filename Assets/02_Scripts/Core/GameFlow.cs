using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonMaster.Core
{
    // 판을 다시 시작하거나 게임을 끄는 것처럼, 여러 화면에서 똑같이 필요한 동작을 모아둔다.
    //
    // 결과 화면과 일시정지 메뉴가 둘 다 "다시 하기"를 갖고 있는데,
    // 각자 복사해두면 한쪽만 고쳤을 때 조용히 어긋난다.
    // 특히 창고 비우기를 빠뜨리면 지난 판의 적이 새 판에 남는다.
    public static class GameFlow
    {
        // 지금 씬을 처음부터 다시 시작한다
        public static void RestartScene()
        {
            // 멈춰둔 채로 씬을 불러오면 새 판이 멈춘 상태로 시작한다
            Time.timeScale = 1f;

            // 창고는 DontDestroyOnLoad 라 씬을 다시 불러와도 살아남는다.
            // 비우지 않으면 직전 판의 적과 코인이 그대로 남는다
            if (ObjectPool.Instance != null) ObjectPool.Instance.ReleaseAllActive();

            Scene scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.buildIndex);
        }

        // 게임을 끈다
        public static void QuitGame()
        {
            // timeScale 은 정적 값이라 멈춘 채로 끝내면 다음에 켤 때까지 남을 수 있다
            Time.timeScale = 1f;

#if UNITY_EDITOR
            // 에디터에서는 Application.Quit 이 아무 일도 하지 않으므로 플레이 모드를 끈다
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
