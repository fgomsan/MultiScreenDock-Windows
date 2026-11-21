using System;
using System.Globalization;

namespace MultiScreenDock.Core;

public static class L10n
{
    public static AppLanguage? OverrideLanguageForTesting { get; set; }

    public static AppLanguage DetectLanguage(string cultureName)
    {
        string code = (cultureName ?? "").ToLowerInvariant();
        if (code.StartsWith("es")) return AppLanguage.Es;
        if (code.StartsWith("fr")) return AppLanguage.Fr;
        if (code.StartsWith("de")) return AppLanguage.De;
        if (code.StartsWith("pt")) return AppLanguage.Pt;
        if (code.StartsWith("it")) return AppLanguage.It;
        if (code.StartsWith("ja")) return AppLanguage.Ja;
        if (code == "zh" || code.StartsWith("zh-hans") || code.StartsWith("zh-cn") || code.StartsWith("zh-sg"))
            return AppLanguage.ZhHans;
        return AppLanguage.En;
    }

    public static AppLanguage SystemDetectedLanguage =>
        DetectLanguage(CultureInfo.CurrentUICulture.Name);

    public static AppLanguage Current
    {
        get
        {
            if (OverrideLanguageForTesting.HasValue)
            {
                return OverrideLanguageForTesting.Value == AppLanguage.System
                    ? SystemDetectedLanguage
                    : OverrideLanguageForTesting.Value;
            }
            var selected = DockSettings.Shared.AppLanguage;
            return selected == AppLanguage.System ? SystemDetectedLanguage : selected;
        }
    }

    private static string Tr(
        string en, string es, string fr, string de,
        string pt, string it, string ja, string zh) =>
        Current switch
        {
            AppLanguage.Es => es,
            AppLanguage.Fr => fr,
            AppLanguage.De => de,
            AppLanguage.Pt => pt,
            AppLanguage.It => it,
            AppLanguage.Ja => ja,
            AppLanguage.ZhHans => zh,
            _ => en
        };

    public static string TooltipStatus => Tr(
        "MultiScreenDock — Smart Multi-Monitor Dock for Windows",
        "MultiScreenDock — Dock inteligente multipantalla para Windows",
        "MultiScreenDock — Dock intelligent multi-écrans pour Windows",
        "MultiScreenDock — Intelligentes Multi-Monitor-Dock für Windows",
        "MultiScreenDock — Dock inteligente multimonitor para Windows",
        "MultiScreenDock — Dock intelligente multi-monitor per Windows",
        "MultiScreenDock — Windows用スマートマルチモニターDock",
        "MultiScreenDock — Windows 智能多显示器程序坞"
    );

    public static string ModeAutoHide => Tr(
        "Auto-hide & reveal on edge hover",
        "Ocultar y mostrar al acercar el ratón al borde",
        "Masquer et afficher au survol du bord",
        "Automatisch ausblenden & am Bildschirmrand einblenden",
        "Ocultar e mostrar ao aproximar o cursor da borda",
        "Nascondi e mostra al passaggio sul bordo",
        "自動的に隠す（画面端にカーソルを合わせて表示）",
        "自动隐藏（鼠标移至屏幕边缘时显示）"
    );

    public static string ModeSmartAutoHide => Tr(
        "Smart auto-hide (only when a window overlaps the Dock)",
        "Ocultación inteligente (solo si una ventana tapa el Dock)",
        "Masquage intelligent (uniquement si une fenêtre couvre le Dock)",
        "Intelligent ausblenden (nur wenn ein Fenster das Dock überlappt)",
        "Ocultação inteligente (apenas quando uma janela cobre o Dock)",
        "Nascondi intelligente (solo se una finestra copre il Dock)",
        "スマート自動非表示（ウィンドウがDockに重なる時のみ隠す）",
        "智能自动隐藏（仅当窗口遮挡程序坞时隐藏）"
    );

    public static string ModeAlwaysVisible => Tr(
        "Keep Dock always fixed & visible",
        "Mantener el Dock siempre fijo y visible",
        "Garder le Dock toujours fixe et visible",
        "Dock immer sichtbar fixieren",
        "Manter o Dock sempre fixo e visível",
        "Mantieni il Dock sempre fisso e visibile",
        "Dockを常に表示したままにする",
        "始终保持程序坞固定可见"
    );

    public static string ShowHintPill => Tr(
        "Show subtle edge indicator when hidden",
        "Mostrar línea guía sutil al estar oculto",
        "Afficher un discret repère lorsque masqué",
        "Dezente Hinweislinie am Rand anzeigen",
        "Mostrar indicador sutil na borda quando oculto",
        "Mostra indicatore discreto quando nascosto",
        "非表示時に控えめなガイドバーを表示",
        "隐藏时在边缘显示柔和提示条"
    );

    public static string PositionMenu => Tr(
        "Position on Screen",
        "Posición en pantalla",
        "Position à l’écran",
        "Position auf dem Bildschirm",
        "Posição na tela",
        "Posizione sullo schermo",
        "画面上の位置",
        "屏幕上的位置"
    );

    public static string PositionBottom => Tr(
        "Bottom", "Abajo", "Bas", "Unten", "Inferior", "In basso", "下", "底部"
    );

    public static string PositionLeft => Tr(
        "Left", "Izquierda", "Gauche", "Links", "Esquerda", "A sinistra", "左", "左侧"
    );

    public static string PositionRight => Tr(
        "Right", "Derecha", "Droite", "Rechts", "Direita", "A destra", "右", "右侧"
    );

    public static string AppearanceMenu => Tr(
        "Appearance", "Apariencia", "Apparence", "Erscheinungsbild", "Aparência", "Aspetto", "外観モード", "外观"
    );

    public static string AppearanceSystemAuto => Tr(
        "System (Follow Windows Light / Dark)",
        "Sistema (Seguir modo claro / oscuro de Windows)",
        "Système (Suivre le mode clair / sombre de Windows)",
        "System (Windows Hell / Dunkel folgen)",
        "Sistema (Seguir modo claro / escuro do Windows)",
        "Sistema (Segui modalità chiara / scura di Windows)",
        "システム設定に従う（ライト / ダーク）",
        "跟随系统（浅色 / 深色）"
    );

    public static string AppearanceLight => Tr(
        "Light Glass", "Cristal claro", "Verre clair", "Helles Glas", "Vidro claro", "Vetro chiaro", "ライトガラス", "浅色毛玻璃"
    );

    public static string AppearanceDark => Tr(
        "Dark Glass", "Cristal oscuro", "Verre sombre", "Dunkles Glas", "Vidro escuro", "Vetro scuro", "ダークガラス", "深色毛玻璃"
    );

    public static string IconSizeMenu => Tr(
        "Icon Size", "Tamaño de iconos", "Taille des icônes", "Symbolgröße", "Tamanho dos ícones", "Dimensione icone", "アイコンのサイズ", "图标大小"
    );

    public static string HoverMagnification => Tr(
        "Smooth 120 Hz hover magnification",
        "Ampliación suave a 120 Hz al pasar el ratón",
        "Agrandissement fluide 120 Hz au survol",
        "Sanfte 120-Hz-Vergrößerung beim Überfahren",
        "Ampliação suave a 120 Hz ao passar o cursor",
        "Ingrandimento fluido a 120 Hz al passaggio",
        "ホバー時の120Hzスムーズ拡大表示",
        "鼠标悬停 120 Hz 平滑放大图标"
    );

    public static string ShowUnpinnedApps => Tr(
        "Show running unpinned apps",
        "Mostrar también apps abiertas no ancladas",
        "Afficher les apps ouvertes non épinglées",
        "Laufende nicht angeheftete Apps anzeigen",
        "Mostrar apps abertos não fixados",
        "Mostra app aperte non fissate",
        "起動中の未固定アプリも表示",
        "显示正在运行但未固定的应用"
    );

    public static string KeepOverFullScreen => Tr(
        "Allow over full-screen apps",
        "Mostrar también sobre apps a pantalla completa",
        "Autoriser sur les apps en plein écran",
        "Auch über Vollbild-Apps anzeigen",
        "Permitir sobre apps em tela cheia",
        "Consenti sopra le app a tutto schermo",
        "フルスクリーンアプリ上でも表示を許可",
        "允许在全屏应用上方显示"
    );

    public static string SkipPrimaryDisplay => Tr(
        "Skip primary display",
        "Omitir en el monitor principal",
        "Ignorer l’écran principal",
        "Hauptdisplay überspringen",
        "Ignorar monitor principal",
        "Ignora schermo principale",
        "メインディスプレイを除外",
        "跳过主显示器"
    );

    public static string StackStyleMenu => Tr(
        "Folder Stack View",
        "Vista al desplegar carpetas",
        "Affichage des piles de dossiers",
        "Ordnerstapel-Ansicht",
        "Visualização de Pilhas",
        "Vista pile cartelle",
        "フォルダスタックの表示形式",
        "文件夹叠放视图"
    );

    public static string StackStyleFan => Tr("Fan", "Abanico", "Éventail", "Fächer", "Leque", "Ventaglio", "ファン", "扇形");
    public static string StackStyleGrid => Tr("Grid", "Cuadrícula", "Grille", "Gitter", "Grade", "Griglia", "グリッド", "网格");
    public static string StackStyleList => Tr("List", "Lista", "Liste", "Liste", "Lista", "Elenco", "リスト", "列表");

    public static string LanguageMenu => Tr(
        "Language", "Idioma", "Langue", "Sprache", "Idioma", "Lingua", "言語 (Language)", "语言 (Language)"
    );

    public static string LaunchAtLogin => Tr(
        "Start with Windows",
        "Iniciar automáticamente con Windows",
        "Démarrer avec Windows",
        "Mit Windows starten",
        "Iniciar com o Windows",
        "Avvia con Windows",
        "Windows起動時に開く",
        "开机自动启动"
    );

    public static string SyncNow => Tr(
        "Sync Dock Icons Now",
        "Sincronizar iconos del Dock ahora",
        "Synchroniser les icônes du Dock maintenant",
        "Dock-Symbole jetzt synchronisieren",
        "Sincronizar ícones do Dock agora",
        "Sincronizza icone del Dock ora",
        "Dockアイコンを今すぐ同期",
        "立即同步程序坞图标"
    );

    public static string FileExplorerLabel => Tr(
        "File Explorer", "Explorador de archivos", "Explorateur de fichiers", "Datei-Explorer", "Explorador de Arquivos", "Esplora file", "エクスプローラー", "文件资源管理器"
    );

    public static string RecycleBinLabel => Tr(
        "Recycle Bin", "Papelera de reciclaje", "Corbeille", "Papierkorb", "Lixeira", "Cestino", "ごみ箱", "回收站"
    );

    public static string EmptyRecycleBin => Tr(
        "Empty Recycle Bin…", "Vaciar Papelera de reciclaje…", "Vider la corbeille…", "Papierkorb leeren…", "Esvaziar Lixeira…", "Svuota Cestino…", "ごみ箱を空にする…", "清空回收站…"
    );

    public static string QuitApp => Tr(
        "Quit MultiScreenDock", "Salir de MultiScreenDock", "Quitter MultiScreenDock", "MultiScreenDock beenden", "Encerrar MultiScreenDock", "Esci da MultiScreenDock", "MultiScreenDock を終了", "退出 MultiScreenDock"
    );
}
