---
"@fake-scope/fake-pkg": patch
---

Create dotnet.yml
Saya akan **lengkapkan fail `rare-hounds-behave.md`** dengan kandungan yang sesuai untuk projek Fairbase. Fail ini boleh berfungsi sebagai dokumentasi atau nota teknikal. Saya akan gunakan tajuk yang relevan dan struktur yang kemas.

---

### 📄 **Kandungan Fail `rare-hounds-behave.md`**

```markdown
# Rare Hounds Behave – Dokumentasi Projek Fairbase

Dokumen ini menerangkan tingkah laku jarang berlaku (*rare hounds behave*) dalam konteks pengurusan data dan infrastruktur projek Fairbase. Ia merangkumi corak, pengecualian, dan cara pengendalian yang tidak lazim tetapi penting untuk diketahui.

## 🎯 Tujuan
- Merekod dan mendokumentasikan kejadian atau tingkah laku sistem yang jarang berlaku.
- Menyediakan panduan penyelesaian masalah untuk situasi luar biasa.
- Memastikan pasukan pembangunan bersedia menghadapi senario yang tidak dijangka.

## 📌 Komponen Berkaitan
Tingkah laku jarang berlaku ini mungkin melibatkan komponen berikut dalam projek Fairbase:
- **BigQuery&DataCoin**: Isu kependaman atau kegagalan pertanyaan.
- **Data&Digital**: Data rosak atau format tidak sah.
- **Python All Language**: Skrip yang gagal dijalankan dalam persekitaran tertentu.
- **MongoDB (Bongo DB)**: Sambungan terputus atau replika lag.
- **GitHub Actions**: Workflow yang gagal secara berselang-seli.
- **React Frontend**: Isu rendering atau cache pelik.

## 🔍 Senario Jarang Berlaku (Rare Hounds)

### 1. **Data Duplikat dalam BigQuery**
- **Gejala**: Rekod yang sama muncul lebih daripada sekali dalam jadual `data_coin`.
- **Punca**: Skrip ETL dijalankan serentak atau kegagalan mekanisme deduplikasi.
- **Tindakan**: Jalankan skrip pembersihan `deduplicate_bigquery.py` (jika ada) atau lakukan manual menggunakan `MERGE` statement.

### 2. **Kegagalan Sambungan MongoDB secara Rawak**
- **Gejala**: Backend API gagal membaca/menulis ke MongoDB pada waktu tertentu, tetapi pulih sendiri.
- **Punca**: Timeout rangkaian atau masalah DNS pada kluster Atlas (jika guna Atlas).
- **Tindakan**: Implementasi semula sambungan dengan retry logic. Tambah log untuk mengesan corak masa.

### 3. **GitHub Actions Workflow "Hantu"**
- **Gejala**: Workflow tiba-tiba berjalan tanpa pencetus yang sepatutnya (contoh: tiada push atau PR).
- **Punca**: Mungkin disebabkan oleh `workflow_dispatch` yang dicetuskan secara manual atau ada ralat dalam konfigurasi `on:`.
- **Tindakan**: Semak semula fail YAML, past tiada `schedule` tersembunyi atau `repository_dispatch` yang tidak dijangka.

### 4. **React App Kosong di Production tetapi OK di Local**
- **Gejala**: Halaman kosong selepas deploy, tetapi `npm start` berfungsi.
- **Punca**: Isu laluan asas (base path) atau pembolehubah persekitaran tidak diisi semasa build.
- **Tindakan**: Semak `package.json` untuk `homepage` atau konfigurasi webpack. Pastikan `REACT_APP_*` dibekalkan dalam GitHub Actions.

### 5. **Skrip Python Tiba-tiba Lambat**
- **Gejala**: Skrip yang biasanya siap dalam 1 minit mengambil masa 10 minit.
- **Punca**: API luaran (seperti BigQuery) mungkin mengalami kependaman, atau data yang diproses membesar tanpa notis.
- **Tindakan**: Tambah pengelogan masa dan pantau saiz dataset. Guna `timeout` dan `retry`.

## 🛡️ Amalan Terbaik untuk Mengendali Rare Hounds
1. **Pengelogan Terperinci**: Pastikan setiap komponen mengeluarkan log yang cukup untuk mengesan punca.
2. **Pemantauan Automatik**: Guna alat seperti Google Cloud Monitoring, Prometheus, atau Uptime Robot.
3. **Dokumentasi Segera**: Setiap kali rare hounds berlaku, rekod dalam fail ini dengan butiran.
4. **Ujian Keteguhan**: Simulasikan kegagalan (contoh: matikan sambungan MongoDB secara manual) dan lihat bagaimana sistem bertindak balas.

## 📅 Sejarah Kemas Kini
| Tarikh       | Perubahan                                      | Pengarang     |
|--------------|------------------------------------------------|---------------|
| 2026-03-16   | Versi awal – dokumentasi rare hounds pertama. | Sazwanismail  |

---

*Nota: Fail ini adalah dokumen hidup – sila tambah senario baru apabila ditemui.*
```

---

## ✅ **Cara Menambah ke Repositori**

### **Pilihan A – Terus di GitHub**
1. Pergi ke repositori `Sazwanismail/Fairbase`.
2. Klik **"Add file"** → **"Create new file"**.
3. Namakan fail: `rare-hounds-behave.md` (boleh letak di root atau dalam folder `docs/`).
4. Tampal kandungan di atas.
5. Tulis mesej commit: "Add rare-hounds-behave.md documentation".
6. Klik **"Commit new file"**.

### **Pilihan B – Melalui Command Line**
```bash
cd Fairbase
nano rare-hounds-behave.md   # atau guna editor lain
# Tampal kandungan, simpan
git add rare-hounds-behave.md
git commit -m "Add rare-hounds-behave.md documentation"
git push origin main
```

---

Sekarang fail `rare-hounds-behave.md` **lengkap** dan sedia untuk digunakan sebagai rujukan pasukan. Jika anda mahu mengubah suai kandungan atau menambah senario lain, sila beritahu saya.
