//---------------------------------------------------------------------------------------
// Copyright (c) 2026 Tristin Wen
// Author: Tristin Wen
// E-Mail: Tristin_Wen@outlook.com
// Date: 2026-07-27
// Desc: Caches generated RuntimeListExample item component references.
//---------------------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;

namespace TristinOrg.VirtualScroll.Sample
{
    /// <summary>
    /// Stores references used while binding a generated sample item.
    /// </summary>
    public sealed class RuntimeListItem : MonoBehaviour, IVirtualScrollItem
    {
        /// <summary>
        /// Gets the RectTransform controlled by the virtual scroll view.
        /// </summary>
        public RectTransform Transform => transform as RectTransform;

        /// <summary>
        /// Generated item label.
        /// </summary>
        public Text Label;
    }
}
