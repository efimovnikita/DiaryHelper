# DiaryHelper

DiaryHelper is an intelligent language journaling Android application built with **C# .NET MAUI**. It assists users in practicing writing personal diary entries in a foreign language with AI-powered grammar checking and contextual guidance from an introspective AI companion (*Kick-Question Bot*).

## Key Features

* **Bilingual Language Practice:** Write your diary in any language supported by Mistral AI, with automatic translation to your native language via Google Translate for peace of mind.
* **Sentence-by-sentence Writing:** Craft your story sentence by sentence. Easily reorder, edit, or delete entries.
* **Intelligent Sentence Correction:** Mistral AI analyzes each sentence, highlighting grammatical corrections with distinct color segmentation.
* **Kick-Question AI Companion:** Stuck with writer's block? Trigger an inspiring, open-ended question tailored to your text and active persona:
  * **Empathetic Friend:** Focus on emotions and wellbeing.
  * **Inquisitive Reporter:** Focus on details, facts, and surroundings.
  * **Reflective Sage:** Focus on lessons, meaning, and values.
  * **Creative Spark:** Focus on creative angles and 'what if' scenarios.
* **Local Offline Storage:** Your journal entries are safely stored on your device using SQLite.
* **Dual Export Modes:** Copy pure user text, or export your story with the AI companion's guiding questions.
* **Secure API Keys:** Google Translate and Mistral API keys are stored encrypted via Android KeyStore (SecureStorage).

## Tech Stack

* **Framework:** .NET 10 MAUI (Android 8.0+)
* **Architecture:** MVVM (CommunityToolkit.Mvvm)
* **Database:** SQLite (sqlite-net-pcl)
* **Cloud Services:** Mistral AI & Google Cloud Translation API
