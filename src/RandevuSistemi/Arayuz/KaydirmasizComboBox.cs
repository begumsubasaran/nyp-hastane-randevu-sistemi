namespace RandevuSistemi.Arayuz;

/// <summary>
/// Fare tekerleğiyle yanlışlıkla değeri değişmeyen açılır kutu.
///
/// Normal ComboBox odaktayken fare tekerleği çevrilirse seçili değer sessizce değişir.
/// Bu sınıf ComboBox'tan TÜRER ve OnMouseWheel metodunu OVERRIDE ederek bu davranışı kapatır
/// (liste açıkken tekerlek yine çalışır). Kalıtım + override için küçük bir örnek.
/// </summary>
public class KaydirmasizComboBox : ComboBox
{
    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if (!DroppedDown && e is HandledMouseEventArgs olay)
        {
            olay.Handled = true; // olayı "işlendi" say; Windows'un varsayılan kaydırması çalışmaz
            return;
        }

        base.OnMouseWheel(e);
    }
}
