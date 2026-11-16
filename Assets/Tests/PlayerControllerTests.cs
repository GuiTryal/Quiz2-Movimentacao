using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PlayerControllerTests
{
    private GameObject ground;
    private PlayerController player;

    [SetUp]
    public void SetUp()
    {
        ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.transform.localScale = new Vector3(50f, 1f, 50f);
        ground.transform.position = new Vector3(0f, -0.5f, 0f);

        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.transform.position = new Vector3(0f, 1.5f, 0f);
        go.AddComponent<Rigidbody>();
        player = go.AddComponent<PlayerController>();
        player.SetCamera(null);
        player.SetExternalInput(Vector2.zero, false);
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(ground);
        Object.Destroy(player.gameObject);
    }

    private static IEnumerator WaitPhysics(int steps)
    {
        for (int i = 0; i < steps; i++) yield return new WaitForFixedUpdate();
    }

    [UnityTest]
    public IEnumerator PersonagemCaiEFicaNoChao()
    {
        yield return WaitPhysics(60);
        Assert.IsTrue(player.IsGrounded, "Deveria estar no chão");
        Assert.AreEqual(1f, player.transform.position.y, 0.1f);
    }

    [UnityTest]
    public IEnumerator PersonagemAndaParaFrente()
    {
        yield return WaitPhysics(50);
        float startZ = player.transform.position.z;

        player.SetExternalInput(new Vector2(0f, 1f), false);
        yield return WaitPhysics(50); // 1 segundo

        float moved = player.transform.position.z - startZ;
        Assert.Greater(moved, 4f, "Deveria andar ~6 m/s para frente");
        Assert.AreEqual(0f, player.transform.position.x, 0.05f);
    }

    [UnityTest]
    public IEnumerator PersonagemPulaEVolta()
    {
        yield return WaitPhysics(50);
        Assert.IsTrue(player.IsGrounded);

        player.SetExternalInput(Vector2.zero, true);
        float maxY = 0f;
        for (int i = 0; i < 20; i++)
        {
            yield return new WaitForFixedUpdate();
            maxY = Mathf.Max(maxY, player.transform.position.y);
        }
        Assert.Greater(maxY, 2.5f, "Deveria subir ~2m");
        Assert.IsFalse(player.IsGrounded);

        yield return WaitPhysics(80);
        Assert.IsTrue(player.IsGrounded, "Deveria voltar ao chão");
    }

    [UnityTest]
    public IEnumerator CenaRealPersonagemPousaNaPlataformaEPula()
    {
        // Remove o setup de teste e carrega a cena de verdade (plataformas do pack)
        Object.Destroy(player.gameObject);
        Object.Destroy(ground);
        yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Movimentacao");

        var scenePlayer = Object.FindObjectOfType<PlayerController>();
        Assert.IsNotNull(scenePlayer);
        yield return WaitPhysics(60);
        Assert.IsTrue(scenePlayer.IsGrounded, "Deveria pousar na plataforma inicial");

        scenePlayer.SetExternalInput(Vector2.zero, true);
        yield return WaitPhysics(15);
        Assert.Greater(scenePlayer.transform.position.y, 2f, "Deveria pular a partir da plataforma");

        // Cria de novo para o TearDown não falhar
        ground = new GameObject();
        player = scenePlayer;
    }

    [UnityTest]
    public IEnumerator NaoPulaNoAr()
    {
        // Coloca o personagem bem alto: ainda estará caindo quando apertar pulo
        yield return new WaitForFixedUpdate();
        player.Body.position = new Vector3(0f, 10f, 0f);
        player.Body.velocity = Vector3.zero;
        yield return WaitPhysics(15);
        Assert.IsFalse(player.IsGrounded);
        player.SetExternalInput(Vector2.zero, true);
        yield return WaitPhysics(2);
        Assert.LessOrEqual(player.Body.velocity.y, 0f, "Não pode pular sem estar no chão");
    }
}
