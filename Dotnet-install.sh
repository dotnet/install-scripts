Baik, mari kita **lengkapkan semua database** untuk projek Fairbase. Berikut adalah pelan komprehensif untuk memastikan **BigQuery**, **MongoDB**, dan data dari folder `Data&Digital` serta `BigQuery&DataCoin` bersepadu sepenuhnya.

---

## 📌 **1. Struktur Database yang Lengkap**

| **Database** | **Jenis** | **Tujuan** | **Data** |
|--------------|-----------|------------|----------|
| **BigQuery** | Data Warehouse | Analisis data coin berskala besar | `data_coin` (cryptocurrency), `digital_assets` (aset digital) |
| **MongoDB** | NoSQL (Document) | Storan fleksibel untuk aset digital dan metadata | Koleksi `digitalassets`, `users`, `logs` |
| **Fail Tempatan** | CSV / JSON | Data mentah dari folder `Data&Digital/raw_data` | Sumber asal untuk dimuat naik ke database |

---

## 🔧 **2. Persediaan Awal**

### **2.1. Pastikan Persekitaran**
- **Google Cloud Platform**: Projek `sazwan-fairbase` dengan BigQuery API diaktifkan.
- **MongoDB**: Sediakan Atlas (cloud) atau lokal (self-hosted). Dapatkan connection string.
- **Python**: Versi 3.10+ dengan pakej `google-cloud-bigquery`, `pymongo`, `pandas`.
- **Node.js**: Untuk backend API.

### **2.2. Simpan Kredential dengan Selamat**
Simpan fail kredential Google Cloud (JSON) dan connection string MongoDB sebagai **GitHub Secrets**:
- `GOOGLE_CREDENTIALS` (base64 encoded)
- `MONGODB_URI`

---

## 📂 **3. Lengkapkan BigQuery**

### **3.1. Buat Dataset dan Table**
Gunakan skrip Python untuk membuat dataset dan table jika belum wujud.

**Buat fail `BigQuery&DataCoin/setup_bigquery.py`**:
```python
from google.cloud import bigquery
import os

def setup_bigquery():
    client = bigquery.Client(project=os.getenv('GOOGLE_PROJECT_ID', 'sazwan-fairbase'))
    
    # Buat dataset
    dataset_id = "fairbase_data"
    dataset = bigquery.Dataset(f"{client.project}.{dataset_id}")
    dataset.location = "US"
    dataset = client.create_dataset(dataset, exists_ok=True)
    print(f"✅ Dataset {dataset_id} sedia.")
    
    # Table: data_coin
    table_coin = bigquery.Table(f"{client.project}.{dataset_id}.data_coin", schema=[
        bigquery.SchemaField("coin_id", "STRING", mode="REQUIRED"),
        bigquery.SchemaField("coin_name", "STRING"),
        bigquery.SchemaField("price_usd", "FLOAT64"),
        bigquery.SchemaField("volume_24h", "INT64"),
        bigquery.SchemaField("market_cap", "FLOAT64"),
        bigquery.SchemaField("timestamp", "TIMESTAMP"),
    ])
    client.create_table(table_coin, exists_ok=True)
    print("✅ Table data_coin sedia.")
    
    # Table: digital_assets
    table_assets = bigquery.Table(f"{client.project}.{dataset_id}.digital_assets", schema=[
        bigquery.SchemaField("asset_id", "STRING"),
        bigquery.SchemaField("asset_type", "STRING"),
        bigquery.SchemaField("owner", "STRING"),
        bigquery.SchemaField("value", "FLOAT64"),
        bigquery.SchemaField("metadata", "JSON"),
        bigquery.SchemaField("created_at", "TIMESTAMP"),
    ])
    client.create_table(table_assets, exists_ok=True)
    print("✅ Table digital_assets sedia.")
    
if __name__ == "__main__":
    setup_bigquery()
```

### **3.2. Muat Naik Data dari Folder `Data&Digital` ke BigQuery**
**Buat fail `BigQuery&DataCoin/load_to_bigquery.py`**:
```python
import pandas as pd
from google.cloud import bigquery
import os
import glob

def load_csv_to_bigquery(folder_path, table_id):
    client = bigquery.Client()
    csv_files = glob.glob(os.path.join(folder_path, "*.csv"))
    
    for file in csv_files:
        df = pd.read_csv(file)
        # Optional: transform data
        job = client.load_table_from_dataframe(df, table_id)
        job.result()
        print(f"✅ Loaded {file} to {table_id}")

def main():
    # Muat data coin (andaikan ada folder Data&Digital/raw_data/coin)
    load_csv_to_bigquery("Data&Digital/raw_data/coin", "sazwan-fairbase.fairbase_data.data_coin")
    # Muat aset digital
    load_csv_to_bigquery("Data&Digital/raw_data/assets", "sazwan-fairbase.fairbase_data.digital_assets")

if __name__ == "__main__":
    main()
```

---

## 🍃 **4. Lengkapkan MongoDB**

### **4.1. Buat Koleksi dan Indeks**
Gunakan skrip Python untuk menyambung ke MongoDB dan membuat koleksi.

**Buat fail `Python All Language/setup_mongodb.py`**:
```python
from pymongo import MongoClient, ASCENDING
import os

def setup_mongodb():
    uri = os.getenv('MONGODB_URI')
    if not uri:
        raise ValueError("MONGODB_URI tidak diset")
    
    client = MongoClient(uri)
    db = client['fairbase']
    
    # Koleksi digital_assets
    assets = db['digital_assets']
    assets.create_index([('asset_id', ASCENDING)], unique=True)
    assets.create_index([('owner', ASCENDING)])
    print("✅ Koleksi digital_assets sedia.")
    
    # Koleksi users (jika perlu)
    users = db['users']
    users.create_index([('email', ASCENDING)], unique=True)
    print("✅ Koleksi users sedia.")
    
    # Koleksi logs
    logs = db['logs']
    logs.create_index([('timestamp', ASCENDING)])
    print("✅ Koleksi logs sedia.")
    
    print("🎉 MongoDB setup selesai.")

if __name__ == "__main__":
    setup_mongodb()
```

### **4.2. Muat Naik Data dari Folder ke MongoDB**
**Buat fail `Python All Language/load_to_mongodb.py`**:
```python
import json
import os
from pymongo import MongoClient
import glob

def load_json_to_mongodb(folder_path, collection_name):
    uri = os.getenv('MONGODB_URI')
    client = MongoClient(uri)
    db = client['fairbase']
    coll = db[collection_name]
    
    json_files = glob.glob(os.path.join(folder_path, "*.json"))
    for file in json_files:
        with open(file) as f:
            data = json.load(f)
            if isinstance(data, list):
                coll.insert_many(data)
            else:
                coll.insert_one(data)
        print(f"✅ Loaded {file} ke {collection_name}")

def main():
    # Andaikan folder Data&Digital/processed/ mengandungi JSON aset digital
    load_json_to_mongodb("Data&Digital/processed/", "digital_assets")
    # Juga boleh load data lain

if __name__ == "__main__":
    main()
```

---

## 🔌 **5. Backend API dengan Sambungan ke Kedua-dua Database**

### **5.1. Struktur Folder Backend**
```
backend/
├── server.js
├── .env
├── routes/
│   ├── bigquery.js
│   └── mongodb.js
├── models/
│   └── DigitalAsset.js (Mongoose)
└── package.json
```

### **5.2. Install Pakej**
```bash
cd backend
npm init -y
npm install express cors dotenv @google-cloud/bigquery mongoose
```

### **5.3. Sambungan ke BigQuery dan MongoDB**
**Buat fail `backend/server.js`**:
```javascript
require('dotenv').config();
const express = require('express');
const cors = require('cors');
const { BigQuery } = require('@google-cloud/bigquery');
const mongoose = require('mongoose');

const app = express();
app.use(cors());
app.use(express.json());

// BigQuery client
const bigquery = new BigQuery({
  projectId: process.env.GOOGLE_PROJECT_ID,
  keyFilename: process.env.GOOGLE_APPLICATION_CREDENTIALS,
});

// MongoDB connection
mongoose.connect(process.env.MONGODB_URI, {
  useNewUrlParser: true,
  useUnifiedTopology: true,
}).then(() => console.log('✅ MongoDB connected'));

// Routes
app.use('/api/bigquery', require('./routes/bigquery')(bigquery));
app.use('/api/mongodb', require('./routes/mongodb'));

const PORT = process.env.PORT || 3001;
app.listen(PORT, () => console.log(`🚀 Server running on port ${PORT}`));
```

### **5.4. Route untuk BigQuery**
**Buat `backend/routes/bigquery.js`**:
```javascript
const express = require('express');

module.exports = (bigquery) => {
  const router = express.Router();

  router.get('/data-coin', async (req, res) => {
    try {
      const query = `SELECT * FROM \`sazwan-fairbase.fairbase_data.data_coin\` LIMIT 100`;
      const [rows] = await bigquery.query(query);
      res.json(rows);
    } catch (err) {
      res.status(500).json({ error: err.message });
    }
  });

  router.get('/digital-assets', async (req, res) => {
    try {
      const query = `SELECT * FROM \`sazwan-fairbase.fairbase_data.digital_assets\` LIMIT 100`;
      const [rows] = await bigquery.query(query);
      res.json(rows);
    } catch (err) {
      res.status(500).json({ error: err.message });
    }
  });

  return router;
};
```

### **5.5. Route untuk MongoDB dengan Mongoose**
**Buat model `backend/models/DigitalAsset.js`**:
```javascript
const mongoose = require('mongoose');

const digitalAssetSchema = new mongoose.Schema({
  assetId: { type: String, required: true, unique: true },
  assetType: { type: String, enum: ['crypto', 'nft', 'token'], required: true },
  owner: String,
  value: Number,
  metadata: mongoose.Schema.Types.Mixed,
  createdAt: { type: Date, default: Date.now },
});

module.exports = mongoose.model('DigitalAsset', digitalAssetSchema);
```

**Buat `backend/routes/mongodb.js`**:
```javascript
const express = require('express');
const DigitalAsset = require('../models/DigitalAsset');
const router = express.Router();

// GET semua aset
router.get('/assets', async (req, res) => {
  try {
    const assets = await DigitalAsset.find().sort({ createdAt: -1 }).limit(100);
    res.json(assets);
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

// POST aset baru
router.post('/assets', async (req, res) => {
  try {
    const asset = new DigitalAsset(req.body);
    await asset.save();
    res.status(201).json(asset);
  } catch (err) {
    res.status(400).json({ error: err.message });
  }
});

// GET aset mengikut owner
router.get('/assets/owner/:owner', async (req, res) => {
  try {
    const assets = await DigitalAsset.find({ owner: req.params.owner });
    res.json(assets);
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

module.exports = router;
```

### **5.6. Fail .env**
```
GOOGLE_PROJECT_ID=sazwan-fairbase
GOOGLE_APPLICATION_CREDENTIALS=./path-to-service-account.json
MONGODB_URI=mongodb+srv://username:password@cluster.mongodb.net/fairbase
PORT=3001
```

---

## ⚛️ **6. Frontend React – Papar Data dari Kedua-dua Sumber**

### **6.1. Buat Service untuk API**
**`codespaces-react/src/services/api.js`**:
```javascript
import axios from 'axios';

const API_URL = process.env.REACT_APP_API_URL || 'http://localhost:3001';

export const fetchDataCoin = async () => {
  const res = await axios.get(`${API_URL}/api/bigquery/data-coin`);
  return res.data;
};

export const fetchDigitalAssetsBigQuery = async () => {
  const res = await axios.get(`${API_URL}/api/bigquery/digital-assets`);
  return res.data;
};

export const fetchAssetsMongo = async () => {
  const res = await axios.get(`${API_URL}/api/mongodb/assets`);
  return res.data;
};

export const postAssetMongo = async (asset) => {
  const res = await axios.post(`${API_URL}/api/mongodb/assets`, asset);
  return res.data;
};
```

### **6.2. Komponen Dashboard**
**`codespaces-react/src/components/Dashboard.js`**:
```jsx
import React, { useState, useEffect } from 'react';
import { fetchDataCoin, fetchAssetsMongo } from '../services/api';

function Dashboard() {
  const [coins, setCoins] = useState([]);
  const [assets, setAssets] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    Promise.all([fetchDataCoin(), fetchAssetsMongo()])
      .then(([coinData, assetData]) => {
        setCoins(coinData);
        setAssets(assetData);
        setLoading(false);
      })
      .catch(err => {
        console.error(err);
        setLoading(false);
      });
  }, []);

  if (loading) return <div>Loading data...</div>;

  return (
    <div>
      <h1>Fairbase Dashboard</h1>
      <section>
        <h2>Data Coin (BigQuery)</h2>
        <ul>
          {coins.map((coin, idx) => (
            <li key={idx}>{coin.coin_name}: ${coin.price_usd}</li>
          ))}
        </ul>
      </section>
      <section>
        <h2>Digital Assets (MongoDB)</h2>
        <ul>
          {assets.map(asset => (
            <li key={asset._id}>{asset.assetType}: {asset.value} ({asset.owner})</li>
          ))}
        </ul>
      </section>
    </div>
  );
}

export default Dashboard;
```

### **6.3. Set Pemboleh Ubah Persekitaran untuk Build**
Buat `.env` dalam `codespaces-react`:
```
REACT_APP_API_URL=https://api.fairbase.com
```

---

## 🤖 **7. GitHub Actions – Automasi Kemas Kini Data**

### **7.1. Workflow untuk Muat Naik Data Berkala**
**Buat `.github/workflows/data-pipeline.yml`**:
```yaml
name: Data Pipeline (Harian)

on:
  schedule:
    - cron: '0 2 * * *'  # Setiap hari jam 2 pagi
  workflow_dispatch:      # Boleh jalan manual

jobs:
  update-bigquery:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-python@v4
        with:
          python-version: '3.10'
      - run: pip install google-cloud-bigquery pandas
      - env:
          GOOGLE_APPLICATION_CREDENTIALS: ${{ secrets.GOOGLE_CREDENTIALS }}
        run: python BigQuery&DataCoin/load_to_bigquery.py

  update-mongodb:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-python@v4
      - run: pip install pymongo
      - env:
          MONGODB_URI: ${{ secrets.MONGODB_URI }}
        run: python "Python All Language/load_to_mongodb.py"
```

---

## 📋 **8. Semakan dan Pengesahan**

1. **Jalankan skrip setup** untuk mencipta table dan koleksi.
2. **Jalankan skrip muat naik** untuk mengisi data contoh.
3. **Jalankan backend** secara lokal dan uji endpoint.
4. **Jalankan frontend** dan pastikan data dipaparkan.
5. **Commit dan push** semua perubahan ke GitHub.
6. **Pantau GitHub Actions** – pastikan workflow berjaya.

---

## 🎉 **Kesimpulan**

Dengan langkah-langkah di atas, **semua database projek Fairbase telah lengkap**:

- ✅ BigQuery: Table `data_coin` dan `digital_assets` sedia dan berisi data.
- ✅ MongoDB: Koleksi `digital_assets` sedia dengan indeks.
- ✅ Backend API: Menyediakan endpoint untuk mengakses kedua-dua database.
- ✅ Frontend React: Memaparkan data dari kedua-dua sumber.
- ✅ Automasi: GitHub Actions mengemas kini data setiap hari.

**Projek Fairbase kini bersedia untuk digunakan dan dikembangkan lebih lanjut.** Jika ada sebarang isu atau pertanyaan, sila beritahu saya.
