# Game Jam Projesi — Teknik Brief

**Motor:** Unity · **Süre:** 48 saat · **Ekip:** 2 kişi (biri dev + 3B sanatçı, biri dev) · **Araç:** Tiger I

## 1. Oyun Nedir

2. Dünya Savaşı, Kuzey Afrika cephesi. Oyuncu, düşman hatlarının gerisinde kalmış bir Tiger tankının sürücüsüdür. Amaç: İngiliz devriyelerine yakalanmadan çölü geçip haritanın ucundaki güvenli limana ulaşmak.

Tür: birinci şahıs, gizlilik odaklı araç sürüş oyunu.
Tek oturumda 6-8 dakikada bitmeli. Tek el yapımı harita, prosedürel üretim yok.

Temel gerginlik: **görmek için hareket etmen gerekir, hareket etmek seni ele verir.** Motor gürültü çıkarır, tank arkasında toz bırakır, sürücü vizörü ise dünyanın sadece dar bir şeridini gösterir.

Önemli tasarım kararı: **oyuncunun kendi tankı modellenmeyecek.** Kamera her zaman vizörün içindedir; dışarısı hiç görülmez. Vizör, ekranın üzerine bindirilmiş 2B bir maskeden ibarettir. Sanatçının tüm zamanı karşı taraftaki İngiliz tanklarına ve araziye gider.

## 2. MVP Kapsamı

MVP'de **olacak** olanlar:

- Sürücü vizöründen birinci şahıs görüş (2B maske katmanı)
- Tank hareketi (ileri, geri, sağa/sola dönüş)
- `M` tuşuyla açılan/kapanan harita
- Sabit ya da tek doğrusal rotada devriye gezen düşman tankları
- Fark edilme (spotting) sistemi ve kaybetme durumu
- Hedef bölgeye ulaşınca kazanma durumu

MVP'de **olmayacak**, ancak vakit kalırsa eklenecek olanlar (öncelik sırasıyla):

1. Toz izi ve motor gürültüsünün fark edilmeye etkisi
2. `Q/A` `E/D` palet kolu kontrol şeması (ayarlardan seçilir)
3. `S` ile koltuk seçim ekranı; komutan, gözcü, telsizci istasyonları

Hiç yapılmayacaklar: ateş etme, balistik, hasar modeli, yakıt, motor arızası, düşman kovalama yapay zekâsı, ana menü, kayıt sistemi, prosedürel arazi, gece-gündüz döngüsü.

Bu liste bir tercih değil, sınırdır. 48 saatte tek gerçek risk kapsamın büyümesidir.

## 3. Kontroller

| Tuş | İşlev |
|---|---|
| `W` / `S` | İleri / geri |
| `A` / `D` | Sola / sağa dön |
| `M` | Haritayı aç/kapat |
| `R` | Yeniden başlat |

Hareket kodu girdiyi doğrudan okumasın; `leftTrackInput` ve `rightTrackInput` (her biri -1 ile 1 arası) değerlerini alsın. Basit şema bu iki değeri `W/A/S/D`'den hesaplar. Palet kolu şeması sonradan eklenirse hareket kodu hiç değişmez.

## 4. Kod Mimarisi

Sınıf ve değişken isimleri İngilizce, yorum satırları Türkçe.

- `TankController` — `leftTrackInput` / `rightTrackInput` alır, Rigidbody üzerinden hareketi uygular. Girdiyi kendisi okumaz.
- `InputRouter` — klavyeyi okur, aktif şemaya göre iki palet değerine çevirir, aktif istasyona yönlendirir.
- `StationManager` — o an hangi istasyonda olunduğunu tutan basit durum makinesi. MVP'de tek değer alır: `Driver`. Kamera konumu ve hangi girdilerin dinleneceği buradan belirlenir. MVP'de gereksiz görünse de baştan yazılsın; aksi halde diğer koltuklar sonradan eklenemez.
- `EnemySpotter` — her düşman aracında bulunur. Mesafe, görüş konisi ve engel kontrolüne göre bir `suspicion` sayacını doldurur. Sayaç dolarsa oyun kaybedilir.
- `NoiseEmitter` — hıza göre 0-1 arası `noiseLevel` üretir, `EnemySpotter` bunu çarpan olarak kullanır. (İkinci öncelik.)
- `MapUI` — `M` ile açılan Canvas katmanı.
- `GameManager` — kazanma/kaybetme, yeniden başlatma.

**Kural:** Girdi okuma, oyun mantığı ve görsel katman ayrı sınıflarda kalsın. Fark edilme kararı `EnemySpotter` içinde verilsin, kamera ya da UI kodunda değil.

## 5. Unity'ye Özel Kararlar

Bu maddeler 48 saatlik sürede en çok zaman kaybettiren noktalardır:

- **WheelCollider kullanma.** Paletli araç için tasarlanmamıştır, ayar yapmak saatler alır ve tank sürekli takla atar. Bunun yerine tek bir `Rigidbody` kullan; ileri hareket için `transform.forward` yönünde hız uygula, dönüş için `transform.Rotate` ya da `Rigidbody.MoveRotation`. Fizik gerçekçiliği bu oyunun konusu değil.
- **Render pipeline'ı ilk saatte seç ve bir daha değiştirme.** URP öneriyorum; sis ve mesafe ayarları hazır gelir. Proje ortasında geçiş yapmak tüm materyalleri bozar.
- **Görüş konisi için özel çözüm yazma.** `Vector3.Angle` ile açı kontrolü, ardından `Physics.Raycast` ile arada engel var mı kontrolü. On satır kod yeter.
- **Harita ekranını kamera render'ıyla yapma.** Sahnenin üstten alınmış tek bir ekran görüntüsünü doku olarak kullan, üzerine oyuncunun konumunu gösteren bir nokta koy. Konumu, dünya koordinatlarını harita dikdörtgenine oranlayarak hesapla.
- **Arazi için Unity Terrain kullan** ama detaya girme. Düz bir zemin, birkaç yükselti, dağınık kaya prefab'ları. Sis mesafesini kısa tut; hem atmosfer verir hem performans sorunu bırakmaz.
- **Build'i ilk gün al.** Jam'lerin en klasik ölümü, son saatte build'in patlamasıdır. Özellikle WebGL build'i Unity'de uzun sürer ve beklenmedik hatalar verir. İlk gün boş sahneyle bir kez build alıp itch.io'ya yükleyin, çalıştığını görün.

## 6. Sanatçı İçin İş Listesi

Öncelik sırasıyla:

1. **Tek bir İngiliz tank modeli** (Crusader ya da Matilda II). Sahnede 4-5 kez tekrar kullanılacak, renk varyasyonuyla farklılaştırılacak. İkinci bir model yapma.
2. Vizör maskesi: ekranın üzerine binen, dar yatay açıklıklı PNG. Kenarları karanlık, hafif çizik ve toz dokusu.
3. Kaya ve kum tepesi prefab'ları (3-4 çeşit yeter, döndürüp ölçekleyerek çoğaltılır).
4. Harita görseli: kâğıt dokulu, elle çizilmiş hissi veren üstten görünüm.
5. Kazanma/kaybetme ekranı.

Oyuncunun kendi tankı, tank içi, mürettebat modelleri: **yok**. Bunlara zaman harcanmayacak.

## 7. 48 Saatlik Plan

**0-2. saat** — Unity projesi, git repo, pipeline kararı, geçici küple hareket eden bir prototip.
**2-5. saat** — Kamera vizör konumuna alınır, maske eklenir, hareket hissi ayarlanır. Bu aşamada oyun zaten "sürülebilir" olmalı.
**5-9. saat** — `EnemySpotter`, görüş konisi, fark edilme sayacı, tek düşman aracıyla test.
**9-12. saat** — Kazanma/kaybetme, yeniden başlatma, ilk build ve itch.io'ya deneme yüklemesi.
**Uyku.**
**13-20. saat** — Harita ekranı, arazinin elle doldurulması, düşman yerleşimi.
**20-28. saat** — Zorluk dengesi. Baştan sona en az on kez oyna. Kaybedilebilir ama adaletsiz hissettirmeyen bir rota kur.
**28-36. saat** — Ses. Motor sesi, palet gıcırtısı, uzaktan gelen düşman motoru, sessizlik anları. Bu oyunda ses oynanışın yarısıdır, sona bırakılmamalı; geçici sesler ilk günden konmalı.
**36-42. saat** — Sanatçının modelleri sahneye entegre edilir, geçici kutular değiştirilir.
**42-46. saat** — Son build, itch.io sayfası, açıklama metni, ekran görüntüleri.
**46-48. saat** — Dokunma. Bu iki saat, bir şeylerin bozulması ihtimali için ayrılmıştır.

## 8. Çalışma Biçimi

Proje adım adım ilerleyecek. Her aşama için önce ne yapılacağını ve nedenini kısaca anlat, sonra kodu ver. Bir aşama bitince **dur ve test etmemi bekle**; ben sonucu bildirmeden sonraki aşamaya geçme.

Açıklamalarında yaptığın seçimin nedenini, varsa alternatif yaklaşımı ve yeni başlayanların o noktada sık yaptığı hatayı da belirt. Açıklamalar kısa ve öz olsun.

Görsel varlıklar en sona kalır; her şey geçici kutularla test edilir.
