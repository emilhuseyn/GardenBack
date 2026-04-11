using App.Core.Entities;
using App.Core.Entities.Identity;
using App.Core.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.Presistence
{
    /// <summary>
    /// bagca (1).xlsx faylındakı məlumatları DB-yə yükləyir.
    /// Mövcud qeydlər saxlanılır (ad+qrup üzrə dublikat yoxlanılır).
    /// </summary>
    public static class ExcelDataSeeder
    {
        // (Qrup adı, Bölmə kodu, Valideyn adı, Uşaq adı, Telefon, Tam gün?, Aylıq məbləğ, Ödəniş günü)
        private record R(string G, string D, string P, string C, string Ph, bool Full, decimal Fee, int PDay);

        private static readonly R[] Data =
        [
            // ── owls (rus) ──────────────────────────────────────────────────────────
            new("owls","rus","Noskova Valeriya",       "Liçina Bella",            "79645760666",  false, 450, 14),
            new("owls","rus","Abasova Tamam",           "Abasova Emiliya",         "0503030400",   true,  800,  1),
            new("owls","rus","Hüseynova Nərmin",        "Hüseynov Camal",          "0509993309",   false, 400, 11),
            new("owls","rus","Bolotina Medina",         "Bolotin Rafael",          "0517777078",   true,  800,  7),
            new("owls","rus","Müzəfərzadə Milana",      "Müzəffərzadə Leysan",     "0508066001",   true,  800, 12),
            new("owls","rus","Məmmədova Aynur",         "Məmmədov Timur",          "0502596767",   false, 450, 12),
            new("owls","rus","Ələsgərova Fəridə",       "Ələsgərova Adelin",       "0516660866",   true,  800, 16),
            new("owls","rus","Əzimova Nərmin",          "Zeynallı Murad",          "0506271116",   false, 450,  2),
            new("owls","rus","Markelova Yekaterina",    "Markelov Aleksandr",      "79166591333",  false, 450,  6),
            new("owls","rus","Məmmədova Zivər",         "Məmmədova Ayan",          "0502660627",   false, 450, 10),
            new("owls","rus","Yaqubova Səbinə",         "Yaqubova Məryəm",         "0518770077",   true,  650, 10),
            new("owls","rus","Pənahova Səma",           "Pənah Selin",             "0507012111",   true,  700,  1),
            new("owls","rus","Nəcəfli Nigar",           "Əliyev Camal",            "0556800003",   false, 450, 28), // was 31
            new("owls","rus","Məmmədova Fəridə",        "Məmmədova Məryəm",        "0508280809",   true,  800,  3),

            // ── bees (rus) ──────────────────────────────────────────────────────────
            new("bees","rus","Quliyeva Günel",          "Quliyev Çinar",           "0552700555",   true,  700, 17),
            new("bees","rus","Bakıxan Xanım",           "Bakıxan Alpaslan",        "0702330204",   true,  700, 25),
            new("bees","rus","İbrahimova Günel",        "Qədirova Solmaz",         "0502282292",   true,  700,  1),
            new("bees","rus","Kazımzadə Aidə",          "Qədimli Ziba",            "0518066891",   true,  800, 25),
            new("bees","rus","Məmmədova İlahə",         "Məmmədov Camal",          "0502108165",   true,  700,  3),
            new("bees","rus","Əliyeva Rəna",            "Mirzəyeva Leyli",         "0502897202",   true,  800, 10),
            new("bees","rus","Hacıyeva Şəbnəm",         "Canməmmədov Raul",        "0502077047",   true,  800, 28),
            new("bees","rus","Hüseynli Bahar",          "Hüseynli Ayla",           "0502277221",   true,  700,  4),
            new("bees","rus","Nuri Nuşabə",             "Nuri Alina",              "0507820606",   true,    0,  1),
            new("bees","rus","Dadaşova Günel",          "Dadaşova Ceyla",          "0503223220",   false, 450,  8),
            new("bees","rus","Qurbanzadə Mintac",       "Qurbanzadə Milana",       "0502113811",   true,  700,  1),
            new("bees","rus","Lətifzadə Nərmin",        "Lətifzadə Sofiya",        "0507105004",   false, 450, 28), // was 30
            new("bees","rus","Ələsgərova Ayla",         "Quliyev Teoman",          "0515835154",   false, 450,  8),
            new("bees","rus","Abbaszadə Jasmin",        "Abbaszadə Atilla",        "0514007722",   true,  800, 28), // was 30

            // ── apples (eng) ────────────────────────────────────────────────────────
            new("apples","eng","Əsgərzadə Nigar",       "Əsgərzadə Büsə",          "0105050999",   true,  900, 12),
            new("apples","eng","Əfəndiyeva Humay",      "Əfəndiyeva İzel",         "0514135533",   true,  900,  8),
            new("apples","eng","Catelani Natalya",      "Catelani Gabriel",        "0519646410",   true,  200, 23),
            new("apples","eng","Əfəndiyeva Lalə",       "Hüseynov Adil",           "0552508329",   true,  900,  9),
            new("apples","eng","Denment Sayajan",       "Denment Abzal",           "77753660415",  true,  900,  8),
            new("apples","eng","Kazımova Aydan",        "Kazımova Sunay",          "0507346523",   false, 500, 19),
            new("apples","eng","Abbasova Cəmilə",       "Abbasova Fatimə",         "0502312361",   true,  800,  1),
            new("apples","eng","Bağırlı Əfşən",         "Bağırlı Ayla",            "0507505093",   true,  800,  1),
            new("apples","eng","Tağızadə Nigar",        "Tağızadə Cahan",          "0502324242",   false, 450,  1),
            new("apples","eng","Kərimli Fidan",         "Kərimli Safiya",          "0502333655",   true,  800, 11),
            new("apples","eng","Bəşirli Elvin",         "Bəşirli Züleyha",         "0502200043",   true,  800,  1),
            new("apples","eng","Əmirullayeva Arifə",    "Əmirullayev Rəvan",       "0558242555",   true,  800,  9),
            new("apples","eng","Məhərrəmova Zemfira",   "Həsənova Məryəm",         "0502247810",   true,  800, 28),
            new("apples","eng","Şahinzadə Günel",       "Şahinzadə İlham",         "0502252883",   true,  900, 15),
            new("apples","eng","Nəsrullayeva Faiq",     "Nəsrullayev Tamerlan",    "380634508613", true,  900,  4),
            new("apples","eng","Suleymanova Nuray",     "Suleymanova Sofiya",      "0502091519",   false, 500,  1),
            new("apples","eng","Abbasova Nərminə",      "Abbasova Cahan",          "0553763336",   true,  800,  1),
            new("apples","eng","Abbasova Nərminə",      "Abbasova Nazel",          "0553763336",   true,  800,  1),
            new("apples","eng","Nəcəfova Şəfa",         "Nəcəfov Toğrul",          "0502048817",   false, 500, 20),
            new("apples","eng","Axundova Lalə",         "Axundova Nilay",          "0502117714",   true,  900, 22),
            new("apples","eng","Həmidova Ülkər",        "Giray Tuana",             "0552850450",   true,  900, 27),
            new("apples","eng","Hüseynova Cəmilə",      "Mckan Georgie",           "0556321000",   true,  900, 10),
            new("apples","eng","Çələbiyeva Nərmin",     "Çələbi Şahin",            "0517315989",   true,  900, 28), // was 30

            // ── rainbow (rus) ────────────────────────────────────────────────────────
            new("rainbow","rus","Əzizbəyova-Sarabskaya İzzətxanım","Əliyev Ayxan", "0506200086",   true,  800,  8),
            new("rainbow","rus","Cəfərova Səbinə",      "Mənsurov Camal",          "0503247371",   true,  700, 15),
            new("rainbow","rus","Seyfulina ALila",      "Abbaslı Almila",          "0554718277",   true,  800,  1),
            new("rainbow","rus","Tahirzadə Şəlalə",     "Tahirzadə Bahar",         "0102210204",   true,  800,  8),
            new("rainbow","rus","Rəhimova Ləman",       "Rəhimov Vaqif",           "0503952939",   true,  700, 12),
            new("rainbow","rus","Rəsulzadə Nailə",      "Rəsulzadə Jasmin",        "0507159057",   true,  800, 15),
            new("rainbow","rus","Sadıqov Nicat",        "Sadıqova Nərgiz",         "0503414040",   true,  700, 15),
            new("rainbow","rus","Quliyeva Aysel",       "Quliyev Tahir",           "0508002822",   true,  800,  1),
            new("rainbow","rus","Həsənova Vüsalə",      "Həsənov Turqut",          "0502499999",   true,  800,  1),
            new("rainbow","rus","Sultanova Fidan",      "Sultanov Ömər",           "0507312757",   true,  700, 15),
            new("rainbow","rus","Kərimova Vüsalə",      "Kərimov Emin",            "0773584125",   true,  800, 17),
            new("rainbow","rus","Sadıqova Jalə",        "Sadıqov Sahib",           "0502105568",   true,  800,  3),
            new("rainbow","rus","Mənsimli Günay",       "Mənsimli Tufan",          "0515140451",   true,  800,  2),
            new("rainbow","rus","Ələkpərova Leyla",     "Ələkpərova Sara",         "0502472323",   false, 450, 13),
            new("rainbow","rus","Süleymanlı Viktoriya", "Süleymanlı Geray",        "0509996106",   true,  800, 14),
            new("rainbow","rus","Gülgəzli Gövhər",      "Gülgəzli Burla",          "0505086313",   true,  800,  1),

            // ── ladybirds (rus) ──────────────────────────────────────────────────────
            new("ladybirds","rus","Əliyeva Mətanət",    "Əliyev Cihan",            "0552997373",   false, 450, 15),
            new("ladybirds","rus","Məmmədov Elçin",     "Məmmədova Ahu",           "0502119489",   true,  650,  5),
            new("ladybirds","rus","Cabbarova Gülnar",   "Cabbarov Ayaz",           "0502736668",   true,  800,  1),
            new("ladybirds","rus","Cəfərova Nigar",     "Cəfərov Fərhad",          "0502959059",   true,  700,  1),
            new("ladybirds","rus","Mahmudova Aygün",    "Əliyeva Leyli",           "0506989104",   false, 400,  2),
            new("ladybirds","rus","Mənsurov Xasay",     "Mənsurova Ahu",           "0552712002",   true,  800,  8),
            new("ladybirds","rus","Vəlibəyova Aytac",   "Əliyeva Ayəl",            "0503073353",   false, 400,  8),
            new("ladybirds","rus","Əliyeva Ülviyyə",    "Mirzəzadə Elyar",         "0502124385",   true,  700, 10),
            new("ladybirds","rus","Vəlizadə Aysel",     "Vəlizadə Kamal",          "0772011555",   true,  800, 11),
            new("ladybirds","rus","Uzun Emin-Perit",    "Uzun Nart",               "0507770303",   true,  700,  1),
            new("ladybirds","rus","Əliyeva Lalə",       "Əliyev Əli",              "0508822211",   true,  700, 10),
            new("ladybirds","rus","Əliyeva Lalə",       "Əliyeva Leyla",           "0508822211",   true,  700, 10),
            new("ladybirds","rus","Behbudova Nuranə",   "Behbudova Fidan",         "0516094956",   true,  800,  2),
            new("ladybirds","rus","Abbasova Aygün",     "Abbasova Emiliya",        "0552278899",   true,  700,  4),
            new("ladybirds","rus","Əlibəyli Böyükxanım","Əlibəyli Bahar",          "0502274445",   false, 400,  8),
            new("ladybirds","rus","Əlibəyli Böyükxanım","Əlibəyli Məryəm",         "0502274445",   false, 450,  8),
            new("ladybirds","rus","Xalıdlı Qəmər",      "Xalidli Azər",            "0505332100",   false, 750, 28), // was 29
            new("ladybirds","rus","Kərimli Sevinc",     "Kərimli Əlişah",          "0552597766",   true,  800,  1),
            new("ladybirds","rus","Məmmədova Solmaz",   "Məmmədova Həvva",         "0557068555",   true,  700,  1),
            new("ladybirds","rus","Nəsirova Gülzar",    "Nəsirova Cəmilə",         "0515374775",   true,  700, 17),
            new("ladybirds","rus","Nuriyev Asif",       "Nuriyeva Humay",          "0512264429",   true,  700, 25),
            new("ladybirds","rus","Mirzəyeva Günay",    "Mirzəyev Altay",          "0512355271",   true,  800, 15),
            new("ladybirds","rus","Əhmədova Teymur",    "Əhmədova Jasmin",         "0702007876",   false, 450,  1),
            new("ladybirds","rus","Quluzadə Şəhanə",    "Quluzadə Azər",           "0507000044",   true,  700,  1),

            // ── smarties (eng) ───────────────────────────────────────────────────────
            new("smarties","eng","Ağazadə Ceylan Günel","Nüsrət Kuzey Ceylan",     "5539331253",   false, 500, 11),
            new("smarties","eng","Cəfərova Sausan",      "Cəfərov Xalid",           "0504660646",   true,    0,  1),
            new("smarties","eng","Azaqova Lalə",        "Azaqov Mərd",             "0502996515",   false, 450,  6),
            new("smarties","eng","Nəsibova Aysel",      "Nəsibova Ameli",          "0502756357",   false, 500,  2),
            new("smarties","eng","Əlizadə Gülüş",       "Rza Cavad",               "0503942070",   true,  900,  1),
            new("smarties","eng","Axundzadə Səma",      "Axundzadə Davud",         "0504904545",   true,  900, 14),
            new("smarties","eng","Əsgərli Tərlalə",     "Əsgərli Alparslan",       "0516994945",   true,  350, 25),
            new("smarties","eng","Aladhami Noor",       "Aladhami ELias",          "0103258516",   false, 500,  2),
            new("smarties","eng","Hümbətova Aytən",     "Hümbətov Alparslan",      "0551006640",   true,  800,  6),
            new("smarties","eng","Amerjanova Saule",    "Nuqumanov İmran",         "0518861467",   true,  800,  1),
            new("smarties","eng","Hüseynzadə Nəzrin",   "Hüseynzadə Umay",         "0102141199",   false, 500, 28), // was 31

            // ── butterflies (rus) ────────────────────────────────────────────────────
            new("butterflies","rus","Məmmədov Elçin",   "Məmmədova Asya",          "0502119489",   true,  650,  5),
            new("butterflies","rus","Əsgərova Aytən",   "Əsgərova Şəms",           "0504049978",   true,  700,  1),
            new("butterflies","rus","Cəfərzadə Zəmanə", "Cəfərzadə Asya",          "0502444491",   true,  750,  1),
            new("butterflies","rus","Ağayeva Natəvan",  "Ağayeva Mələk",           "0558688816",   true,  800,  2),
            new("butterflies","rus","Qasımova İzzət",   "Bababəyov Osman",         "0557576407",   true,  700,  8),
            new("butterflies","rus","Kara Beyim",       "Kara Alparslan",          "0513013656",   true,  700,  1),
            new("butterflies","rus","Məmmədova Zivər",  "Məmmədov İsa",            "0502660627",   true,  800, 10),
            new("butterflies","rus","Rumyanceva Kristina","Rumyanceva Polina",      "0557283626",   true,  800, 27),
            new("butterflies","rus","Mahmudova Zümrüd", "Mahmudov Ziya",           "0558942030",   true,  800, 15),
            new("butterflies","rus","Nematova Lalita",  "Nematova Melina",         "0508284747",   true,  800, 15),
            new("butterflies","rus","Quliyeva Fidan",   "Quliyev Camal",           "0515152222",   false, 400, 24),
            new("butterflies","rus","Hüseynova Nərmin", "Hüseynov Cəlil",          "0509993309",   true,  800,  3),
            new("butterflies","rus","Dilbazi Könül",    "Dilbazi Əsəd",            "0502560167",   true,  800, 20),
            new("butterflies","rus","Qəhrəmanova Kəmalə","Qəhrəmanov Cahangir",    "0508769988",   true,  800,  1),
            new("butterflies","rus","Əliyeva Fəridə",   "Əliyev Kamil",            "0502905347",   true,  800,  1),
            new("butterflies","rus","Talıblı Fidan",    "Talıblı Fuad",            "0503903127",   true,  700,  1),
            new("butterflies","rus","Babazadə Ülkər",   "Babazadə Mənsur",         "0502818811",   false, 450, 10),
            new("butterflies","rus","Hacıyeva Nərmin",  "Hacıyeva Sofiya",         "0502310460",   true,  800,  1),
            new("butterflies","rus","Həsənova Aygün",   "Həsənov Maqsud",          "0552141100",   true,  700,  4),
            new("butterflies","rus","Məmmədova Günel",  "Məmmədov İskəndər Əli",   "0515540999",   true,  800,  1),
            new("butterflies","rus","Aslanova Lalə",    "Qasımov Aydın",           "0502101087",   true,  700,  1),
            new("butterflies","rus","Həsənova Günel",   "Həsənova Sofiya",         "0514358030",   true,  700,  2),
            new("butterflies","rus","Şəfiyeva Roza",    "Şəfi Əsəd",               "0503693253",   true,  700,  8),

            // ── goldfish (rus) ───────────────────────────────────────────────────────
            new("goldfish","rus","Yusifova Nigar",      "Yusifova Alida",          "0502003757",   true,  750,  1),
            new("goldfish","rus","Şərifova Familla",    "Şərifov Raul",            "0509668867",   true,  700,  1),
            new("goldfish","rus","Əliyeva Zöhrə",       "Əliyeva Kamilla",         "0553313636",   true,  700, 15),
            new("goldfish","rus","Hüseynli Bahar",      "Hüseynli Mikayıl",        "0502277221",   true,  700, 28),
            new("goldfish","rus","Cəfərli Umay",        "Həsənli Aliya",           "0505592223",   true,  800,  8),
            new("goldfish","rus","Bağırova Cəmilə",     "Bağərova Cənnət",         "0777000827",   true,  700,  6),
            new("goldfish","rus","Həsənova Vüsalə",     "Həsənov Malik",           "0508871817",   true,  700, 15),
            new("goldfish","rus","Əhədzadə Qönçə",      "Əhədzadə Vuqar",          "0507560056",   false, 400,  1),
            new("goldfish","rus","Hacıyeva Sevda",      "Hacıyeva Amina",          "0704775757",   false, 400, 15),
            new("goldfish","rus","İsayeva Narmina",     "İsayeva Afina",           "0513299707",   true,  700, 15),
            new("goldfish","rus","Zülfüqarova Səbinə",  "Zülfüqarov David",        "0503453305",   true,  800,  1),
            new("goldfish","rus","Sadırov Nicat",       "Sadıqova Nargül",         "0503414040",   true,  700, 15),
            new("goldfish","rus","Xəlilova Kamilla",    "Xəlilov Şahin",           "0502870570",   false, 400,  1),
            new("goldfish","rus","Əhmədova Aydan",      "Əhmədov Murad",           "0503277711",   true,  700,  1),
            new("goldfish","rus","Zeynalova Cəmilə",    "Zeynalov Daniel",         "0772500827",   true,  700,  1),
            new("goldfish","rus","Şport Olqa",          "Şport Miron",             "79625798285",  false, 400,  1),
            new("goldfish","rus","Məhərrəmova Pərvin",  "Məhərrəmova Sürə",        "0504330006",   true,  700,  1),
            new("goldfish","rus","Əliyeva Aytən",        "Əliyev Cəlil",            "0502248228",   true,    0,  1),
            new("goldfish","rus","Səlimova Nigar",      "Nağıyev Cavad",           "0554140041",   true,  700,  1),
            new("goldfish","rus","Dadaşova Günel",      "Bağırova Cahan",          "0504441505",   true,  700,  1),
            new("goldfish","rus","Umudlu Təranə",       "Umudlu İsmayıl",          "0502102039",   true,  800, 15),
            new("goldfish","rus","Səidzadə Rəna",       "Əsgərov Ammar",           "0503202888",   true,  700,  1),
            new("goldfish","rus","Ağayeva Nəzrin",      "Ağayeva Mehin",           "0502270919",   true,  700,  2),
            new("goldfish","rus","Kazımova Aysel",      "Kazımova Mehin",          "0502500767",   true,  700,  8),

            // ── starfish (eng) ───────────────────────────────────────────────────────
            new("starfish","eng","Ana Karen Hernandez", "Ana Lucia Castillo Hernandez","0502453664",false,500,  1),
            new("starfish","eng","Əmrahova Sona",       "İbrahimova Sofiya",       "0515155242",   true,  800,  1),
            new("starfish","eng","Babayeva Xətayə",     "Babayeva Samira",         "0552017555",   true,  850, 25),
            new("starfish","eng","Babayeva Xətayə",     "Babayeva Sofiya",         "0552017555",   true,  850, 25),
            new("starfish","eng","İsmayılova Gülnar",   "İsmayılov Tunar",         "0555065500",   true,  800, 25),
            new("starfish","eng","Kərimova Aydan",      "Kərimova Neqin",          "0553103300",   true,  900,  1),
            new("starfish","eng","Sayyadova Regina",    "Sayyadova Safina",        "0506896114",   true,  900,  1),
            new("starfish","eng","Aladhami Noor",       "Aladhami Haya",           "0103258516",   false, 450,  2),
            new("starfish","eng","Əliyeva Müjgan",      "Əliyeva Sara",            "0502863408",   true,  800,  1),
            new("starfish","eng","İsmayılzadə Viktoriya","İsmayılzadə Amina",      "0998101114",   true,  850, 10),
            new("starfish","eng","Haciyev Urfan",       "Hacıyev Sübhan",          "0504232393",   false, 450,  8),
            new("starfish","eng","Rəsulzadə Ləman",     "Rəsulzadə İmran",         "0508085474",   true,  800, 22),
            new("starfish","eng","Amanova Fəridə",      "Amanova Nərgiz",          "0509740904",   false, 500,  5),
            new("starfish","eng","Abilli Sevda",        "Abilli Selin",            "0509905151",   true,  900,  1),
            new("starfish","eng","Hacızadə Aytəkin",    "Hacızadə Miray",          "0559918668",   true,  900, 27),
            new("starfish","eng","Abdullayeva",         "Abdullayeva Sara",        "0506310121",   false, 450,  2),
            new("starfish","eng","Abdullayeva",         "Abdullayeva Sofiya",      "0506310121",   false, 450,  2),
            new("starfish","eng","Bəşirli Elvin",       "Bəşirli Leyli",           "0502420043",   true,  700,  1),
            new("starfish","eng","Yusilfli Gülşad",     "Yusifli Yasmin",          "0502284018",   false, 500, 10),

            // ── stars (rus) ──────────────────────────────────────────────────────────
            new("stars","rus","Hüseynzadə Həbib",       "Hüseynzadə Selin",        "0512670000",   true,  800,  4),
            new("stars","rus","Xalidli Qəmər",          "Xalidli Alparslan",       "0505332100",   true,  750, 28), // was 29
            new("stars","rus","Cəfərzadə Zəmanə",       "Cəfərzadə Sofiya",        "0502444491",   true,  750,  1),
            new("stars","rus","Bayramova Xəyalə",       "Bayramov Adnan",          "0502113719",   true,  800, 28),
            new("stars","rus","Musayeva Könül",         "Musayeva Azeliya",        "79111145050",  true,  750,  1),
            new("stars","rus","Musayeva Könül",         "Musayev Ruslan",          "79111145050",  true,  750,  1),
            new("stars","rus","Dadaşova Sübhanə",       "Dadaşpva Aliya",          "0557698108",   false, 400, 23),
            new("stars","rus","Cəfərova Nigar",         "Cəfərov Nəriman",         "0505640049",   true,  700,  1),
            new("stars","rus","Yusifova Nigar",         "Yusifova Elina",          "0502003757",   true,  750,  1),
            new("stars","rus","Həmidova Aygün",         "Termurov Emil",           "0502804754",   false, 450, 15),
            new("stars","rus","Sultanlı Nəzrin",        "Sultanlı Heydər",         "0504304320",   false, 450, 18),
            new("stars","rus","Əliyeva Aytən",          "Əliyev Ömər",             "0503647757",   true,  700, 15),
            new("stars","rus","Həsənzadə Səma",         "Həsənzadə Adel",          "0504006014",   true,  700,  1),
            new("stars","rus","Həsənova Aysel",         "Həsənov Cavad",           "0505730050",   true,  700,  8),
            new("stars","rus","Salmanlı Ülviyyə",       "Salmanlı Davud",          "0502857172",   true,  800,  6),
            new("stars","rus","Abasova Ulyana",          "Abasova İnci",            "0516350713",   true,    0,  1),

            // ── clever (eng) ─────────────────────────────────────────────────────────
            new("clever","eng","Sadıqov Xəyyam",        "Sadıqov Maqsud",          "0502310488",   true,  900,  1),
            new("clever","eng","Cəfərli Günay",         "Xankışızadə Selin",       "0502247419",   true,  900, 28),
            new("clever","eng","Piriyev Kənan",         "Piriyev Kamal",           "0514090470",   true,  800,  1),
            new("clever","eng","Miar Rontisi",          "Evan Kassis",             "0502870205",   true,  800,  1),
            new("clever","eng","İsmayılzadə Viktoriya", "İsmayılzadə Məryəm",      "0998101114",   true,  850, 10),
            new("clever","eng","Paşabəyli Fuad",        "Paşabəyli Dəniz",         "0514336451",   true,  900,  8),
            new("clever","eng","Abdullayeva Tərlan",    "Abdullayev Tahir",        "0508410045",   true,  800,  1),
            new("clever","eng","Rzayeva Əfsanə",        "Rsayeva Asu",             "0502462244",   true,  800,  2),
            new("clever","eng","Kürçaylı Leyla",        "Kürçaylı Bahar",          "0554809820",   true,  800,  2),
            new("clever","eng","Amerjanova Saule",      "Nuqumanov Raxmatullah",   "0518861467",   true,  900,  1),
            new("clever","eng","Muxtarlı Nərmin",       "Muxtarlı Mikayıl",        "0504005445",   true,  900,  5),
            new("clever","eng","Məmmədova Nigar",       "Məmmədov Ənvər",          "0502125543",   false, 500,  3),
            new("clever","eng","Haciyeva Aytən",        "Haciyev Cavad",           "0516286970",   false, 500, 28),
            new("clever","eng","Şahbazi Cahan",         "Allahverdiyev İslam",     "0509996006",   true,  900,  1),
            new("clever","eng","Zeynalova Nərminə",     "Zeynalov Abdullah",       "0705161640",   true,  800, 23),
            new("clever","eng","Quluzadə Şəhanə",       "Quluzadə Emilia",         "0507000044",   true,  800,  1),
            new("clever","eng","Məlikova Vəfa",         "Məlikov Murad",           "0553501550",   true,  800, 22),
            new("clever","eng","Kərimli Baba",          "Kərimli Alparslan",       "0507875766",   true,  800, 15),
            new("clever","eng","Əliyeva Vəfa",          "Əliyev Adəm",             "0503203232",   false, 500, 22),
            new("clever","eng","Əsgərova Aytən",        "Əsgərov Murad",           "0504049978",   true,  800,  1),
        ];

        // Hər qrupa aid tərbiyəçilərin tam adları (Soyad Ad formatı)
        private static readonly Dictionary<string, string[]> GroupTeachers =
            new(StringComparer.OrdinalIgnoreCase)
        {
            ["owls"]        = ["Əliyeva Səbinə",      "Əliyeva Nuranə"],
            ["bees"]        = ["Muhacirova İlhamə",    "Bayramova Nailə",    "İsaqova Esmira"],
            ["apples"]      = ["Abasova Ulyana",       "Müslimova Gülnar",   "Məmmədova Fəridə"],
            ["rainbow"]     = ["Sultanova Samira",     "Hüseynova Aytən",    "Quliyeva Zenfira"],
            ["ladybirds"]   = ["Hüseynova Rəna",       "Mirzəyeva Esmira",   "Rəsulova İndira",   "Rəcəbova Sevda"],
            ["smarties"]    = ["Hüseynzadə Fatimə",    "Çukarina Mariya",    "Məmmədova Arzu"],
            ["butterflies"] = ["Əhmədova Jalə",        "Əliyeva Yeganə"],
            ["goldfish"]    = ["Şabanova Sonaxanım",   "Axmadullina Rufina", "Əhmədova Cəmilə",   "Alimova Mədinə"],
            ["starfish"]    = ["Mirzəyeva Leyla",      "Hüseynova Zərifə",   "Mahmudova Günay"],
            ["stars"]       = ["Şatalina Viktoriya",   "Huseynzadə Səfayə",  "Əhmədova Svetlana"],
            ["clever"]      = ["Nuriyeva Qızaman",     "Hacıbalayeva Erena", "Məmmədzadə Ətirə"],
        };

        // Face/turnstile sistemindən gələn PersonId uyğunluqları (ad+soyad üzrə)
        private static readonly Dictionary<string, int> PersonIdsByName = new(StringComparer.Ordinal)
        {
            [PersonKey("Ceyla", "Dadaşova")] = 49,
            [PersonKey("Çinar", "Quliyev")] = 52,
            [PersonKey("Leyli", "Mirzəyeva")] = 53,
            [PersonKey("Milana", "Qurbanzadə")] = 54,
            [PersonKey("Emiliya", "Abbasova")] = 55,
            [PersonKey("Ayla", "Hüseynli")] = 56,
            [PersonKey("Raul", "Canməmmədov")] = 57,
            [PersonKey("Solmaz", "Qədirova")] = 59,
            [PersonKey("Camal", "Məmmədov")] = 60,

            [PersonKey("Şəms", "Əsgərova")] = 139,
            [PersonKey("Sofiya", "Hacıyeva")] = 141,
            [PersonKey("Maqsud", "Həsənov")] = 142,
            [PersonKey("Asya", "Məmmədova")] = 143,
            [PersonKey("Fuad", "Talıblı")] = 144,
            [PersonKey("Kamil", "Əliyev")] = 145,
            [PersonKey("Ziya", "Mahmudov")] = 148,
            [PersonKey("Asya", "Cəfərzadə")] = 150,
            [PersonKey("Sofiya", "Həsənova")] = 151,
            [PersonKey("Mənsur", "Babazadə")] = 152,
            [PersonKey("Cəlil", "Hüseynov")] = 155,
            [PersonKey("Melina", "Nematova")] = 157,
            [PersonKey("İsa", "Məmmədov")] = 159,

            [PersonKey("İzel", "Əfəndiyeva")] = 214,
            [PersonKey("Adil", "Hüseynov")] = 216,
            [PersonKey("Abzal", "Denment")] = 217,
            [PersonKey("Fatimə", "Abbasova")] = 219,
            [PersonKey("Safiya", "Kərimli")] = 222,
            [PersonKey("Rəvan", "Əmirullayev")] = 223,
            [PersonKey("Məryəm", "Həsənova")] = 225,
            [PersonKey("İlham", "Şahinzadə")] = 226,
            [PersonKey("Tamerlan", "Nəsrullayev")] = 227,
            [PersonKey("Cahan", "Abbasova")] = 228,
            [PersonKey("Nazel", "Abbasova")] = 229,
            [PersonKey("Toğrul", "Nəcəfov")] = 230,
            [PersonKey("Büsə", "Əsgərzadə")] = 235,
            [PersonKey("Sübhan", "Hacıyev")] = 237,
            [PersonKey("Tunar", "İsmayılov")] = 238,
        };

        public static async Task SeedAsync(AppDbContext context, UserManager<User> userManager)
        {
            Console.WriteLine("[ExcelSeed] Başlayır...");

            // 1. Bölmələri tap və ya yarat
            var rusDivId = await EnsureDivisionAsync(context, "Rus bölməsi",     "Rus");
            var engDivId = await EnsureDivisionAsync(context, "İngilis bölməsi", "İngilis");

            // 2. Hər qrup üçün tərbiyəçiləri yarat, birincisini əsas müəllim kimi təyin et
            var groupPrimaryTeacher = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (groupName, teachers) in GroupTeachers)
            {
                string? primaryId = null;
                foreach (var teacherName in teachers)
                {
                    var tid = await EnsureTeacherAsync(userManager, teacherName);
                    primaryId ??= tid;
                }
                groupPrimaryTeacher[groupName] = primaryId!;
            }

            // 3. Qrupları tap və ya yarat
            var groupIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var groupName in Data.Select(d => d.G).Distinct())
            {
                var divCode   = Data.First(d => d.G == groupName).D;
                var divId     = divCode == "eng" ? engDivId : rusDivId;
                var language  = divCode == "eng" ? "İngilis" : "Rus";
                var teacherId = groupPrimaryTeacher.TryGetValue(groupName, out var tid) ? tid
                    : await context.Users.Where(u => u.Role == EUserRole.Teacher).Select(u => u.Id).FirstOrDefaultAsync()
                      ?? throw new InvalidOperationException("Heç bir müəllim tapılmadı.");
                groupIds[groupName] = await EnsureGroupAsync(context, groupName, divId, teacherId, language);
            }

            // 4. Uşaqları əlavə et (dublikat yoxlanılır: ad + qrup)
            int added = 0, skipped = 0;
            foreach (var row in Data)
            {
                var groupId = groupIds[row.G];
                var (firstName, lastName) = SplitName(row.C);
                var phone    = NormalizePhone(row.Ph);
                var schedule = row.Full ? ScheduleType.FullDay : ScheduleType.HalfDay;
                var payDay   = Math.Min(row.PDay, 28);
                var personId = ResolvePersonId(firstName, lastName);

                var existingChild = await context.Children.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(c => c.FirstName == firstName && c.LastName == lastName && c.GroupId == groupId);

                if (existingChild != null)
                {
                    if (personId.HasValue && existingChild.PersonId != personId.Value)
                        existingChild.PersonId = personId.Value;

                    skipped++;
                    continue;
                }

                context.Children.Add(new Child
                {
                    FirstName        = firstName,
                    LastName         = lastName,
                    DateOfBirth      = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    GroupId          = groupId,
                    ScheduleType     = schedule,
                    MonthlyFee       = row.Fee,
                    PaymentDay       = payDay,
                    ParentFullName   = row.P,
                    ParentPhone      = phone,
                    RegistrationDate = DateTime.UtcNow,
                    Status           = ChildStatus.Active,
                    PersonId         = personId,
                });
                added++;
            }

            await context.SaveChangesAsync();
            Console.WriteLine($"[ExcelSeed] Tamamlandı. Əlavə edildi: {added}, keçildi: {skipped}");
        }

        // ── helpers ─────────────────────────────────────────────────────────────────

        private static async Task<int> EnsureDivisionAsync(AppDbContext ctx, string name, string lang)
        {
            var div = await ctx.Divisions.IgnoreQueryFilters().FirstOrDefaultAsync(d => d.Name == name);
            if (div is not null) return div.Id;

            div = new Division { Name = name, Language = lang };
            ctx.Divisions.Add(div);
            await ctx.SaveChangesAsync();
            Console.WriteLine($"[ExcelSeed] Bölmə yaradıldı: {name}");
            return div.Id;
        }

        private static async Task<int> EnsureGroupAsync(AppDbContext ctx, string name, int divId, string teacherId, string language)
        {
            var group = await ctx.Groups.IgnoreQueryFilters().FirstOrDefaultAsync(g => g.Name == name);
            if (group is not null) return group.Id;

            group = new Group
            {
                Name          = name,
                DivisionId    = divId,
                TeacherId     = teacherId,
                MaxChildCount = 30,
                AgeCategory   = "3-6",
                Language      = language,
            };
            ctx.Groups.Add(group);
            await ctx.SaveChangesAsync();
            Console.WriteLine($"[ExcelSeed] Qrup yaradıldı: {name}");
            return group.Id;
        }

        private static (string First, string Last) SplitName(string fullName)
        {
            // Azərbaycan formatı: "Soyad Ad" — ilk söz soyad, qalanı ad
            var parts = fullName.Trim().Split(' ', 2);
            return parts.Length == 2
                ? (First: parts[1].Trim(), Last: parts[0].Trim())
                : (First: parts[0].Trim(), Last: parts[0].Trim());
        }

        private static async Task<string> EnsureTeacherAsync(UserManager<User> userManager, string fullName)
        {
            var (firstName, lastName) = SplitName(fullName);
            var email = MakeTeacherEmail(firstName, lastName);

            var existing = await userManager.FindByEmailAsync(email);
            if (existing is not null) return existing.Id;

            var user = new User
            {
                UserName       = email,
                Email          = email,
                FirstName      = firstName,
                LastName       = lastName,
                Role           = EUserRole.Teacher,
                IsActive       = true,
                EmailConfirmed = true,
            };
            var result = await userManager.CreateAsync(user, "Teacher@123");
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"Müəllim yaradılmadı: {fullName}. {string.Join(", ", result.Errors.Select(e => e.Description))}");

            Console.WriteLine($"[ExcelSeed] Müəllim yaradıldı: {fullName}");
            return user.Id;
        }

        private static string MakeTeacherEmail(string firstName, string lastName)
        {
            static string Slug(string s)
            {
                var map = new Dictionary<char, char>
                {
                    ['ə'] = 'e', ['Ə'] = 'e', ['ş'] = 's', ['Ş'] = 's',
                    ['ğ'] = 'g', ['Ğ'] = 'g', ['ü'] = 'u', ['Ü'] = 'u',
                    ['ö'] = 'o', ['Ö'] = 'o', ['ç'] = 'c', ['Ç'] = 'c',
                    ['ı'] = 'i', ['İ'] = 'i',
                };
                var sb = new System.Text.StringBuilder();
                foreach (var c in s.ToLowerInvariant())
                {
                    if (map.TryGetValue(c, out var r)) sb.Append(r);
                    else if (char.IsAsciiLetterOrDigit(c))  sb.Append(c);
                }
                return sb.ToString();
            }
            return $"{Slug(lastName)}.{Slug(firstName)}@garden.az";
        }

        private static string NormalizePhone(string raw)
        {
            var ph = raw.Trim().Replace(" ", "").Replace("-", "");

            // 0XXXXXXXXX (10 rəqəm, Azərbaycan lokal) → +994XXXXXXXXX
            if (ph.Length == 10 && ph.StartsWith('0'))
                return "+994" + ph[1..];

            // 7XXXXXXXXXXX (11 rəqəm, Rusiya) → +7XXXXXXXXXX
            if (ph.Length == 11 && ph.StartsWith('7'))
                return "+" + ph;

            // 380XXXXXXXXX (12 rəqəm, Ukrayna) → +380XXXXXXXXX
            if (ph.Length == 12 && ph.StartsWith("380"))
                return "+" + ph;

            // 77XXXXXXXXXX və digər uzun nömrələr
            if (ph.Length >= 11 && !ph.StartsWith('+'))
                return "+" + ph;

            // 10 rəqəm, 0 ilə başlamır (qeyri-standart) → +994 ilə birləşdir
            if (ph.Length == 10 && !ph.StartsWith('+'))
                return "+994" + ph;

            return ph;
        }

        private static int? ResolvePersonId(string firstName, string lastName)
            => PersonIdsByName.TryGetValue(PersonKey(firstName, lastName), out var id) ? id : null;

        private static string PersonKey(string firstName, string lastName)
            => $"{NormalizeName(firstName)} {NormalizeName(lastName)}";

        private static string NormalizeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            return value.Trim().ToLowerInvariant()
                .Replace('ə', 'e').Replace('Ə', 'e')
                .Replace('ş', 's').Replace('Ş', 's')
                .Replace('ğ', 'g').Replace('Ğ', 'g')
                .Replace('ü', 'u').Replace('Ü', 'u')
                .Replace('ö', 'o').Replace('Ö', 'o')
                .Replace('ç', 'c').Replace('Ç', 'c')
                .Replace('ı', 'i').Replace('İ', 'i')
                .Replace('é', 'e').Replace('É', 'e')
                .Replace('á', 'a').Replace('Á', 'a');
        }
    }
}
