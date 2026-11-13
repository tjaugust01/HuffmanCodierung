# Huffman-Codierung (CLI)

Konsolenanwendung zur verlustfreien Kompression und Dekompression beliebiger Dateien auf Byte-Ebene mittels Huffman-Codierung (lauffähig unter **Windows** und **Linux** ohne separate .NET-Installation).

## Testdaten hinterlegen

Eigene Testdateien können an einem der folgenden Orte abgelegt werden:

1. **Im Unterordner `data/`** (z. B. `data/beispiel.txt`) – dann reicht beim Aufruf der reine Dateiname (z. B. `beispiel.txt`) oder `data/beispiel.txt`.
2. **Im aktuellen Arbeitsverzeichnis** neben der Binary (z. B. `./beispiel.txt`).
3. **An einem beliebigen Pfad**, der direkt beim Aufruf angegeben wird.

> **Hinweis:** Wird beim Komprimieren ein Dateiname angegeben, der noch nicht existiert, legt das Programm automatisch eine kleine Beispiel-Textdatei unter diesem Namen im Ordner `data/` an.

## Nutzung der Binaries

### Vorbereitung (nur Linux)

Unter Linux muss die Datei ggf. einmalig ausführbar gemacht werden:

```bash
chmod +x ./HuffmanCodierung
```

### 1. Interaktiver Modus (ohne Parameter)

**Windows:**
```powershell
.\HuffmanCodierung.exe
```

**Linux:**
```bash
./HuffmanCodierung
```

Das Programm fragt anschließend nacheinander:
1. Aktion: `[c]ompress` (`c`) oder `[d]ecompress` (`d`)
2. Dateiname bzw. Pfad zur Datei
3. Bei Kompression: ob die Entropie-Statistik (`-s`) ausgegeben werden soll (`y`/`n`)

### 2. Kommandozeilen-Parameter (CLI)

**Datei komprimieren:**

```bash
# Windows
.\HuffmanCodierung.exe -c beispiel.txt

# Linux
./HuffmanCodierung -c beispiel.txt
```

**Datei komprimieren und Statistik / Entropie-Analyse ausgeben (`-s`):**

```bash
# Windows
.\HuffmanCodierung.exe -c beispiel.txt -s

# Linux
./HuffmanCodierung -c beispiel.txt -s
```

**Datei dekomprimieren:**

```bash
# Windows
.\HuffmanCodierung.exe -d beispiel.txt.huff

# Linux
./HuffmanCodierung -d beispiel.txt.huff
```

### Übersicht der Parameter

| Flag | Alternativen | Beschreibung |
| :--- | :--- | :--- |
| `-c` | `--compress`, `c` | Komprimiert die angegebene Datei in das `.huff`-Format (`<dateiname>.huff`). |
| `-d` | `--decompress`, `d` | Dekomprimiert eine `.huff`-Datei zurück in die Originaldatei (die Endung `.huff` kann optional weggelassen werden). |
| `-s` | `--statistic`, `s` | Gibt nach der Kompression eine Entropie- und Kompressionsstatistik aus. |

## Ausgabedateien

- **Kompression:** Erzeugt im selben Ordner wie die Eingabedatei eine `<dateiname>.huff`-Datei.
- **Dekompression:** Speichert die wiederhergestellte Datei im Ordner der `.huff`-Datei. Existiert dort bereits eine Datei mit dem Originalnamen, wird sie nicht überschrieben, sondern mit dem Zusatz `_decompressed` vor der Dateiendung gespeichert (z. B. `beispiel_decompressed.txt`).
