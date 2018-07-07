using UnityEngine;

namespace RandomPlus {
    public interface IPanel {
        Rect PanelRect {
            get;
        }

        void Resize(Rect rect);
        void Draw();
    }
}
