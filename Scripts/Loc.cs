using Godot;
using System;
using System.Collections.Generic;

// English + European Portuguese strings (same text as the HTML prototype).
public static class Loc
{
    public static string Lang = "en";
    public static event Action Changed;

    static readonly Dictionary<string, string> En = new Dictionary<string, string>
    {
        ["title"] = "Operation Tidy-Up: Mission Poop-astrophe",
        ["hud_title"] = "Operation Tidy-Up\nLevel 1 · The Living Room",
        ["cleaned"] = "Cleaned {n} / {t}", ["snd_on"] = "Sound: on (M)", ["snd_off"] = "Sound: off (M)",
        ["banner"] = "BUBA PANICS!",
        ["tool1"] = "1 · Shovel", ["tool2"] = "2 · Mop", ["tool3"] = "3 · Scraper", ["tool4"] = "4 · Spray",
        ["ermelinda"] = "DONA ERMELINDA", ["boss_wait"] = "She arrives in {n} s", ["boss_hud"] = "Strikes {s}/3 · Spots {n}/{t}",
        ["boss_enter"] = "Dona Ermelinda enters with her white glove! Keep the marked spots spotless!",
        ["boss_ok"] = "Spot approved! {n} to go.", ["boss_strike"] = "She found mess! 'Tsc tsc tsc...' Strike {n}/3",
        ["boss_fail"] = "Three strikes! She leaves with a deep sigh. Try again.", ["buba_surprise"] = "Buba left a new surprise!",
        ["intro4"] = "The inspection! She arrives in {n} s. Clean up, then keep her marked spots and path clear of mess. Three strikes and you're out!",
        ["hud_title4"] = "Operation Tidy-Up\nLevel 4 · The Inspection",
        ["ov_text4"] = "Level 4: The Inspection. Dona Ermelinda walks the living room with a white glove while Buba keeps leaving surprises. Three strikes and she leaves.",
        ["win4_h"] = "PERFECT! Dona Ermelinda is speechless!", ["win4_p"] = "The white glove stayed white. She even smiled. Buba gets a treat. The end!",
        ["lvl_name4"] = "The Inspection",
        ["sprayready"] = "Detergent spray ready! Aim at a puddle or dried mess and press {k}.", ["b_spray"] = "SPRAY",
        ["pr_spray_desk"] = "SPACE · spray ({n} left)", ["pr_spray_touch"] = "SPRAY ({n} left)",
        ["spray_empty"] = "The spray is empty! Use the mop and scraper.", ["spray_none"] = "Nothing to spray there. Aim at a puddle or dried mess.",
        ["crust_soft"] = "Softened! Scrape it now.", ["spray_bar"] = "Spray {n}/{t}",
        ["twist"] = "DING DONG! Dona Ermelinda's car just pulled up outside!", ["kitchen_clock"] = "SHE'S EARLY",
        ["hud_title3"] = "Operation Tidy-Up\nLevel 3 · The Kitchen",
        ["ov_text3"] = "Level 3: The Kitchen. The final stretch! The detergent spray is yours now, but Buba is at his wildest.",
        ["intro3"] = "Final stretch! Spray (4) loosens puddles and dried mess from a distance. Limited charges!",
        ["win3_h"] = "Kitchen spotless!", ["win3_p"] = "But that was her car outside... Dona Ermelinda is at the door with a white glove. The real inspection is next!",
        ["lvl_name3"] = "The Kitchen",
        ["lvl_locked"] = "locked", ["menu_play"] = "Play", ["menu_levels"] = "Levels", ["menu_settings"] = "Settings", ["menu_quit"] = "Quit", ["menu_back"] = "Back", ["menu_main"] = "Main menu",
        ["lvl_name1"] = "The Living Room", ["lvl_name2"] = "The Garden Party",
        ["mm_on"] = "Minimap: on", ["mm_off"] = "Minimap: off",
        ["scraperready"] = "Scraper ready! Dried mess needs scraping first.", ["b_scrape"] = "SCRAPE",
        ["pr_scrape_desk"] = "HOLD SPACE · scrape", ["pr_scrape_touch"] = "HOLD · scrape",
        ["pr_crust_desk"] = "Dried mess! Press 3 for the scraper", ["pr_crust_touch"] = "Dried mess! Tap SCRAPER",
        ["crust_done"] = "Scraped loose! Now scoop it up.", ["dirty_warn"] = "The bucket water is filthy. Refill it at the tap!",
        ["bucket2"] = "Clean water {n}%",
        ["hud_title2"] = "Operation Tidy-Up\nLevel 2 · The Garden Party", ["party_banner"] = "HAPPY BIRTHDAY!",
        ["win2_h"] = "Party saved! Neighbours none the wiser!", ["win2_p"] = "Buba is under the table with a suspicious party hat.",
        ["next_level"] = "Next level", ["level_btn"] = "Level {n}",
        ["intro2"] = "Garden party! Scrape dried mess (3), scoop poops and mop puddles. Buba will panic early!",
        ["ov_title"] = "Operation Tidy-Up", ["ov_sub"] = "Mission Poop-astrophe",
        ["ov_text2"] = "Level 2: The Garden Party. The neighbours' birthday is on and Buba is nervous. Clean fast, he panics early!",
        ["ov_text"] = "Level 1: The Living Room. Mr. Custódio must clean up every one of Buba's surprises before the mother-in-law arrives.",
        ["pa_title"] = "Paused", ["pa_sub"] = "Mr. Custódio is catching his breath", ["pa_text"] = "Buba is waiting. Do not trust that face.",
        ["p1"] = "Buba makes three colours of mess. Scoop each one up, carry it to the bin of the same colour and dump it. Some hide under rugs and the coffee table.",
        ["p2"] = "Beware: puddles grow until you mop them, radioactive poop pulses (hazard bin!), and slippery dirt makes you slide and drop what you carry.",
        ["desk"] = "Walk: W / S  ·  Turn: A / D or mouse  ·  Sidestep: Q / E  ·  Scoop / dump: Space  ·  Tools: 1 shovel, 2 mop (hold Space to scrub)",
        ["touch"] = "Left thumb: up / down walks, left / right turns  ·  Drag on the right to look up / down  ·  Yellow button: scoop / dump (hold to scrub). Tap the SHOVEL / MOP chips to swap tools.",
        ["start"] = "Start", ["start_touch"] = "Tap to start", ["resume"] = "Resume", ["resume_touch"] = "Tap to resume",
        ["win_h"] = "Level Completed! Mother-in-law approved!", ["win_p"] = "Buba is sitting very innocently. Nothing to see here.", ["restart"] = "Restart",
        ["langBtn"] = "Português (PT)",
        ["type_0"] = "brown", ["type_1"] = "green", ["type_2"] = "pink", ["type_3"] = "radioactive", ["hazard"] = "HAZARD", ["dump_here"] = "DUMP HERE",
        ["portrait_banner"] = "EMPLOYEE OF THE MONTH", ["portrait_name"] = "Mr. Custódio",
        ["key_space"] = "SPACE", ["key_tap_btn"] = "tap the button", ["k_prefix_desk"] = "SPACE · ", ["k_prefix_touch"] = "TAP · ", ["the_button"] = "the button",
        ["intro"] = "Scoop poops ({k}) and sort them into the matching bins!",
        ["puddle_hint"] = "Yuck, a puddle! Switch to the mop ({k}).", ["puddle_k_touch"] = "tap MOP", ["puddle_k_desk"] = "press 2",
        ["got"] = "Got a {c} one! Take it to the {c} bin.", ["takeit"] = "Take it to the {c} bin!", ["wrongbin"] = "Wrong bin! A {c} one goes in the {c} bin.",
        ["dumped"] = "Dumped!", ["squeaky"] = "Squeaky clean!", ["togo"] = "{p} {n} to go.",
        ["handsfull"] = "Hands full! Dump what you are carrying first.",
        ["mopready"] = "Mop ready: hold {k} next to a puddle to scrub.", ["shovelready"] = "Shovel ready.",
        ["rev_table"] = "You spotted one hiding under the table!", ["rev_step"] = "Something was hiding under the rug!", ["rev_lift"] = "Found one hiding under the rug!",
        ["slip_carry"] = "Whoa! You slipped and dropped the {c} one!", ["slip_squish"] = "Squish! Slippery!", ["slip_puddle"] = "Whoa! Slippery puddle!",
        ["bump_carry"] = "Buba crashed into you! You dropped the {c} one!", ["bump"] = "Oof! Buba bumped you!",
        ["zap"] = "Bzzt! Radiation pulse! Time your approach between pulses.",
        ["panic"] = "Buba is panicking! Watch the clean spots...",
        ["final"] = "Buba dropped a special golden {c} one! Grab it FAST or it will explode!",
        ["boom"] = "BOOM! It exploded into a puddle and {n} little ones!",
        ["pr_dump"] = "dump in the {c} bin", ["pr_wrong"] = "Wrong bin · find the {c} one", ["pr_carry"] = "Carrying {c} · find the {c} bin",
        ["pr_scrub_touch"] = "HOLD · scrub", ["pr_scrub_desk"] = "HOLD SPACE · scrub",
        ["pr_mopnote"] = "The mop is for puddles · {k}", ["pr_mopnote_touch"] = "tap SHOVEL", ["pr_mopnote_desk"] = "press 1 for the shovel",
        ["pr_scoop"] = "scoop", ["pr_lift"] = "lift the rug", ["pr_puddle"] = "Puddle! {k}", ["pr_puddle_touch"] = "Tap MOP", ["pr_puddle_desk"] = "Press 2 for the mop",
        ["b_dump"] = "DUMP", ["b_scrub"] = "SCRUB", ["b_scoop"] = "SCOOP", ["b_refill"] = "REFILL",
        ["tap_label"] = "TAP", ["tap_here"] = "REFILL HERE", ["bucket"] = "Bucket {n}%",
        ["bucket_empty"] = "Bucket empty! Refill it at the tap.", ["bucket_full"] = "Bucket full.",
        ["pr_refill_touch"] = "HOLD · refill bucket", ["pr_refill_desk"] = "HOLD SPACE · refill bucket",
    };

    static readonly Dictionary<string, string> Pt = new Dictionary<string, string>
    {
        ["title"] = "Operação Limpeza: Missão Cóc-tástrofe",
        ["hud_title"] = "Operação Limpeza\nNível 1 · A Sala de Estar",
        ["cleaned"] = "Limpos {n} / {t}", ["snd_on"] = "Som: ligado (M)", ["snd_off"] = "Som: desligado (M)",
        ["banner"] = "O BUBA ENLOUQUECEU!",
        ["tool1"] = "1 · Pá", ["tool2"] = "2 · Esfregona", ["tool3"] = "3 · Raspador", ["tool4"] = "4 · Spray",
        ["ermelinda"] = "DONA ERMELINDA", ["boss_wait"] = "Ela chega em {n} s", ["boss_hud"] = "Faltas {s}/3 · Locais {n}/{t}",
        ["boss_enter"] = "A Dona Ermelinda entra com a luva branca! Mantém os locais marcados impecáveis!",
        ["boss_ok"] = "Local aprovado! Faltam {n}.", ["boss_strike"] = "Ela encontrou porcaria! 'Tsc tsc tsc...' Falta {n}/3",
        ["boss_fail"] = "Três faltas! Ela sai com um grande suspiro. Tenta outra vez.", ["buba_surprise"] = "O Buba deixou uma nova surpresa!",
        ["intro4"] = "A inspecção! Ela chega em {n} s. Limpa tudo e depois mantém os locais marcados e o caminho dela livres. Três faltas e acabou!",
        ["hud_title4"] = "Operação Limpeza\nNível 4 · A Inspecção",
        ["ov_text4"] = "Nível 4: A Inspecção. A Dona Ermelinda percorre a sala com uma luva branca enquanto o Buba continua a deixar surpresas. Três faltas e ela vai-se embora.",
        ["win4_h"] = "PERFEITO! A Dona Ermelinda ficou sem palavras!", ["win4_p"] = "A luva branca ficou branca. Ela até sorriu. O Buba ganha um petisco. Fim!",
        ["lvl_name4"] = "A Inspecção",
        ["sprayready"] = "Spray de detergente pronto! Aponta a uma poça ou sujidade seca e carrega em {k}.", ["b_spray"] = "SPRAY",
        ["pr_spray_desk"] = "ESPAÇO · pulverizar ({n} restantes)", ["pr_spray_touch"] = "SPRAY ({n} restantes)",
        ["spray_empty"] = "O spray acabou! Usa a esfregona e o raspador.", ["spray_none"] = "Nada para pulverizar ali. Aponta a uma poça ou sujidade seca.",
        ["crust_soft"] = "Amoleceu! Raspa agora.", ["spray_bar"] = "Spray {n}/{t}",
        ["twist"] = "DING DONG! O carro da Dona Ermelinda acabou de chegar!", ["kitchen_clock"] = "ELA CHEGOU CEDO",
        ["hud_title3"] = "Operação Limpeza\nNível 3 · A Cozinha",
        ["ov_text3"] = "Nível 3: A Cozinha. A recta final! Agora tens o spray de detergente, mas o Buba está no seu pior.",
        ["intro3"] = "Recta final! O spray (4) amolece poças e sujidade seca à distância. Cargas limitadas!",
        ["win3_h"] = "Cozinha impecável!", ["win3_p"] = "Mas aquele era o carro dela... A Dona Ermelinda está à porta com a luva branca. A verdadeira inspecção é a seguir!",
        ["lvl_name3"] = "A Cozinha",
        ["lvl_locked"] = "bloqueado", ["menu_play"] = "Jogar", ["menu_levels"] = "Níveis", ["menu_settings"] = "Definições", ["menu_quit"] = "Sair", ["menu_back"] = "Voltar", ["menu_main"] = "Menu principal",
        ["lvl_name1"] = "A Sala de Estar", ["lvl_name2"] = "A Festa no Jardim",
        ["mm_on"] = "Minimapa: ligado", ["mm_off"] = "Minimapa: desligado",
        ["scraperready"] = "Raspador pronto! A sujidade seca tem de ser raspada primeiro.", ["b_scrape"] = "RASPAR",
        ["pr_scrape_desk"] = "SEGURA ESPAÇO · raspar", ["pr_scrape_touch"] = "SEGURA · raspar",
        ["pr_crust_desk"] = "Sujidade seca! Carrega no 3 para o raspador", ["pr_crust_touch"] = "Sujidade seca! Toca em RASPADOR",
        ["crust_done"] = "Soltou-se! Agora apanha-o.", ["dirty_warn"] = "A água do balde está imunda. Enche-o na torneira!",
        ["bucket2"] = "Água limpa {n}%",
        ["hud_title2"] = "Operação Limpeza\nNível 2 · A Festa no Jardim", ["party_banner"] = "PARABÉNS!",
        ["win2_h"] = "Festa salva! Os vizinhos nem desconfiam!", ["win2_p"] = "O Buba está debaixo da mesa com um chapéu de festa suspeito.",
        ["next_level"] = "Próximo nível", ["level_btn"] = "Nível {n}",
        ["intro2"] = "Festa no jardim! Raspa a sujidade seca (3), apanha cocós e esfrega poças. O Buba entra em pânico cedo!",
        ["ov_title"] = "Operação Limpeza", ["ov_sub"] = "Missão Cóc-tástrofe",
        ["ov_text2"] = "Nível 2: A Festa no Jardim. É o aniversário dos vizinhos e o Buba está nervoso. Limpa depressa, ele entra em pânico cedo!",
        ["ov_text"] = "Nível 1: A Sala de Estar. O Sr. Custódio tem de limpar todas as surpresas do Buba antes de a sogra chegar.",
        ["pa_title"] = "Em pausa", ["pa_sub"] = "O Sr. Custódio está a recuperar o fôlego", ["pa_text"] = "O Buba está à espera. Não confies naquela cara.",
        ["p1"] = "O Buba faz porcaria em três cores. Apanha cada uma, leva-a ao caixote da mesma cor e despeja-a. Algumas escondem-se debaixo dos tapetes e da mesa de centro.",
        ["p2"] = "Cuidado: as poças alastram até as esfregares, o cocó radioativo emite pulsos (caixote do perigo!) e a sujidade escorregadia faz-te deslizar e largar o que levas.",
        ["desk"] = "Andar: W / S  ·  Rodar: A / D ou rato  ·  Deslocar: Q / E  ·  Apanhar / despejar: Espaço  ·  Ferramentas: 1 pá, 2 esfregona (segura o Espaço para esfregar)",
        ["touch"] = "Polegar esquerdo: cima / baixo anda, esquerda / direita roda  ·  Arrasta à direita para olhar para cima / baixo  ·  Botão amarelo: apanhar / despejar (segura para esfregar). Toca em PÁ / ESFREGONA para trocar de ferramenta.",
        ["start"] = "Começar", ["start_touch"] = "Toca para começar", ["resume"] = "Continuar", ["resume_touch"] = "Toca para continuar",
        ["win_h"] = "Nível Concluído! Sogra aprovada!", ["win_p"] = "O Buba está sentado, todo inocente. Não há nada para ver aqui.", ["restart"] = "Recomeçar",
        ["langBtn"] = "English",
        ["type_0"] = "castanho", ["type_1"] = "verde", ["type_2"] = "rosa", ["type_3"] = "radioativo", ["hazard"] = "PERIGO", ["dump_here"] = "DESPEJA!",
        ["portrait_banner"] = "FUNCIONÁRIO DO MÊS", ["portrait_name"] = "Sr. Custódio",
        ["key_space"] = "ESPAÇO", ["key_tap_btn"] = "toca no botão", ["k_prefix_desk"] = "ESPAÇO · ", ["k_prefix_touch"] = "TOCA · ", ["the_button"] = "o botão",
        ["intro"] = "Apanha os cocós ({k}) e separa-os pelos caixotes da mesma cor!",
        ["puddle_hint"] = "Eca, uma poça! Muda para a esfregona ({k}).", ["puddle_k_touch"] = "toca em ESFREGONA", ["puddle_k_desk"] = "carrega no 2",
        ["got"] = "Apanhaste um cocó {c}! Leva-o ao caixote {c}.", ["takeit"] = "Leva-o ao caixote {c}!", ["wrongbin"] = "Caixote errado! Um cocó {c} vai para o caixote {c}.",
        ["dumped"] = "Despejado!", ["squeaky"] = "Limpinho!", ["togo"] = "{p} Faltam {n}.",
        ["handsfull"] = "Mãos ocupadas! Despeja primeiro o que levas.",
        ["mopready"] = "Esfregona pronta: segura {k} ao pé de uma poça para esfregar.", ["shovelready"] = "Pá pronta.",
        ["rev_table"] = "Descobriste um escondido debaixo da mesa!", ["rev_step"] = "Havia algo escondido debaixo do tapete!", ["rev_lift"] = "Encontraste um escondido debaixo do tapete!",
        ["slip_carry"] = "Uau! Escorregaste e largaste o cocó {c}!", ["slip_squish"] = "Squish! Escorregadio!", ["slip_puddle"] = "Uau! Poça escorregadia!",
        ["bump_carry"] = "O Buba chocou contigo! Largaste o cocó {c}!", ["bump"] = "Ai! O Buba empurrou-te!",
        ["zap"] = "Bzzt! Pulso de radiação! Aproxima-te entre pulsos.",
        ["panic"] = "O Buba entrou em pânico! Vigia os sítios limpos...",
        ["final"] = "O Buba largou um cocó {c} dourado especial! Apanha-o DEPRESSA ou vai explodir!",
        ["boom"] = "BUM! Explodiu numa poça e em {n} pequeninos!",
        ["pr_dump"] = "despejar no caixote {c}", ["pr_wrong"] = "Caixote errado · procura o {c}", ["pr_carry"] = "A levar cocó {c} · procura o caixote {c}",
        ["pr_scrub_touch"] = "SEGURA · esfregar", ["pr_scrub_desk"] = "SEGURA ESPAÇO · esfregar",
        ["pr_mopnote"] = "A esfregona é para poças · {k}", ["pr_mopnote_touch"] = "toca em PÁ", ["pr_mopnote_desk"] = "carrega no 1 para a pá",
        ["pr_scoop"] = "apanhar", ["pr_lift"] = "levantar o tapete", ["pr_puddle"] = "Poça! {k}", ["pr_puddle_touch"] = "Toca em ESFREGONA", ["pr_puddle_desk"] = "Carrega no 2 para a esfregona",
        ["b_dump"] = "DESPEJAR", ["b_scrub"] = "ESFREGAR", ["b_scoop"] = "APANHAR", ["b_refill"] = "ENCHER",
        ["tap_label"] = "TORNEIRA", ["tap_here"] = "ENCHER AQUI", ["bucket"] = "Balde {n}%",
        ["bucket_empty"] = "Balde vazio! Enche-o na torneira.", ["bucket_full"] = "Balde cheio.",
        ["pr_refill_touch"] = "SEGURA · encher balde", ["pr_refill_desk"] = "SEGURA ESPAÇO · encher balde",
    };

    // Replaces {name} placeholders: Loc.T("got", ("c", "brown"))
    public static string T(string key, params (string, string)[] args)
    {
        var dict = Lang == "pt" ? Pt : En;
        string s;
        if (!dict.TryGetValue(key, out s) && !En.TryGetValue(key, out s)) return key;
        foreach (var a in args) s = s.Replace("{" + a.Item1 + "}", a.Item2);
        return s;
    }

    public static string TypeName(int type) { return T("type_" + type); }

    public static void Init()
    {
        string lang = "";
        var cf = new ConfigFile();
        if (cf.Load("user://settings.cfg") == Error.Ok) lang = cf.GetValue("game", "lang", "").AsString();
        if (lang != "en" && lang != "pt") lang = OS.GetLocale().StartsWith("pt") ? "pt" : "en";
        Lang = lang;
    }

    public static void Set(string lang)
    {
        Lang = lang;
        var cf = new ConfigFile();
        cf.Load("user://settings.cfg");
        cf.SetValue("game", "lang", lang);
        cf.Save("user://settings.cfg");
        if (Changed != null) Changed();
    }

    public static void Toggle() { Set(Lang == "pt" ? "en" : "pt"); }
}
