using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(TMP_Text))]
public class TMPLinkOpener : MonoBehaviour, IPointerClickHandler
{
    private TMP_Text m_TextMeshPro;

    void Awake()
    {
        m_TextMeshPro = GetComponent<TMP_Text>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Find the index of the link that was clicked
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(m_TextMeshPro, eventData.position, null);

        // If the Canvas is NOT set to Screen Space - Overlay, use the main camera instead of null:
        // int linkIndex = TMP_TextUtilities.FindIntersectingLink(m_TextMeshPro, eventData.position, Camera.main);

        if (linkIndex != -1)
        {
            // Get the information about the clicked link
            TMP_LinkInfo linkInfo = m_TextMeshPro.textInfo.linkInfo[linkIndex];

            // Open the URL in the system browser
            Application.OpenURL(linkInfo.GetLinkID());
        }
    }
}
