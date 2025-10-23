// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace Aspire.Hosting.Azure;

// This file contains obsolete elements kept for backward compatibility.

public partial class AIFoundryModel
{
    public static partial class AI21Labs
    {
        /// <inheritdoc cref="AI21Jamba15Large"/>
        [Obsolete("Use AI21Jamba15Large instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly AIFoundryModel Ai21Jamba15Large = new() { Name = "AI21-Jamba-1.5-Large", Version = "1", Format = "AI21 Labs" };

        /// <inheritdoc cref="AI21Jamba15Mini"/>
        [Obsolete("Use AI21Jamba15Mini instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly AIFoundryModel Ai21Jamba15Mini = new() { Name = "AI21-Jamba-1.5-Mini", Version = "1", Format = "AI21 Labs" };
    }

    public static partial class DeepSeek
    {
        /// <inheritdoc cref="DeepSeekR1"/>
        [Obsolete("Use DeepSeekR1 instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly AIFoundryModel DeepseekR1 = new() { Name = "DeepSeek-R1", Version = "1", Format = "DeepSeek" };

        /// <inheritdoc cref="DeepSeekR10528"/>
        [Obsolete("Use DeepSeekR10528 instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly AIFoundryModel DeepseekR10528 = new() { Name = "DeepSeek-R1-0528", Version = "1", Format = "DeepSeek" };

        /// <inheritdoc cref="DeepSeekV3"/>
        [Obsolete("Use DeepSeekV3 instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly AIFoundryModel DeepseekV3 = new() { Name = "DeepSeek-V3", Version = "1", Format = "DeepSeek" };

        /// <inheritdoc cref="DeepSeekV30324"/>
        [Obsolete("Use DeepSeekV30324 instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly AIFoundryModel DeepseekV30324 = new() { Name = "DeepSeek-V3-0324", Version = "1", Format = "DeepSeek" };
    }

    public static partial class Meta
    {
        /// <inheritdoc cref="Llama4Maverick17B128EInstructFP8"/>
        [Obsolete("Use Llama4Maverick17B128EInstructFP8 instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly AIFoundryModel Llama4Maverick17B128EInstructFp8 = new() { Name = "Llama-4-Maverick-17B-128E-Instruct-FP8", Version = "1", Format = "Meta" };
    }

    public static partial class Microsoft
    {
        /// <inheritdoc cref="AzureAIContentSafety"/>
        [Obsolete("Use AzureAIContentSafety instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly AIFoundryModel AzureAiContentSafety = new() { Name = "Azure-AI-Content-Safety", Version = "1", Format = "Microsoft" };

        /// <inheritdoc cref="AzureAIContentSafety"/>
        [Obsolete("Use AzureAIContentSafety instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly AIFoundryModel AzureAiContentUnderstanding = new() { Name = "Azure-AI-Content-Understanding", Version = "1", Format = "Microsoft" };

        /// <inheritdoc cref="AzureAIContentSafety"/>
        [Obsolete("Use AzureAIContentSafety instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly AIFoundryModel AzureAiDocumentIntelligence = new() { Name = "Azure-AI-Document-Intelligence", Version = "1", Format = "Microsoft" };

        /// <inheritdoc cref="AzureAIContentSafety"/>
        [Obsolete("Use AzureAIContentSafety instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly AIFoundryModel AzureAiLanguage = new() { Name = "Azure-AI-Language", Version = "1", Format = "Microsoft" };

        /// <summary>
        /// ## Azure AI Speech ## Introduction The Speech service provides speech to text and text to speech capabilities with a Speech resource. You can transcribe speech to text with high accuracy, produce natural-sounding text to speech voices, translate spoken audio, and use speaker recognition during conversations. Create custom voices, add specific words to your base vocabulary, or build your own models. Run Speech anywhere, in the cloud or at the edge in containers. It&apos;s easy to speech enable your applications, tools, and devices with the [Speech CLI](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/spx-overview), [Speech SDK](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/speech-sdk), and [REST APIs](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/rest-speech-to-text). ## Core Features - **Speech To Text**  - **Description**: Use speech to text to transcribe audio into text, either in real-time or asynchronously with batch transcription. Convert audio to text from a range of sources, including microphones, audio files, and blob storage. Use speaker diarization to determine who said what and when. Get readable transcripts with automatic formatting and punctuation.   The base model might not be sufficient if the audio contains ambient noise or includes numerous industry and domain-specific jargon. In these cases, you can create and train custom speech models with acoustic, language, and pronunciation data. Custom speech models are private and can offer a competitive advantage.  - **Key Features**   - Real Time Speech To Text   - Transcriptions, captions, or subtitles for live meetings   - [Diarization](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/get-started-stt-diarization)   - [Pronunciation assessment](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/how-to-pronunciation-assessment)   - Contact center agents assist   - Dictation   - Voice agents   - Fast Transcription   - Quick audio or video transcription, subtitles, and edit   - Video translation   - Batch Transcription   - Transcriptions, captions, or subtitles for prerecorded audio   - Contact center post-call analytics   - Diarization   - Custom Speech   - Models with enhanced accuracy for specific domains and conditions. - **Text To Speech**  - **Description**: With [text to speech](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/text-to-speech), you can convert input text into human like synthesized speech. Use human-like prebuilt neural voices out of the box in more than 140 locales and 500 voices or create a custom neural voice that&apos;s unique to your product or brand. You can also enhance the voice experience by using together with Text to speech Avatar to convert text to life-like and high-quality synthetic talking avatar videos.   - **Prebuilt neural voice**: Highly natural out-of-the-box voices. Check the prebuilt neural voice samples the [Voice Gallery](https://speech.microsoft.com/portal/voicegallery) and determine the right voice for your business needs.   - **Custom neural voice**: Besides the prebuilt neural voices that come out of the box, you can also create a [custom neural voice](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/custom-neural-voice) that is recognizable and unique to your brand or product. Custom neural voices are private and can offer a competitive advantage. Check the custom neural voice samples [here](https://aka.ms/customvoice).   - **Text to speech Avatar**: You can convert text into a digital video of a photorealistic human (either a prebuilt avatar or a [custom text to speech avatar](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/text-to-speech-avatar/what-is-text-to-speech-avatar#custom-text-to-speech-avatar)) speaking with a natural-sounding voice. It works best with the Azure neural voices.  - **Key Features**   - **Prebuilt neural voice**    - Neural voice (incl. OpenAI-based voices)    - Neural HD voice (incl. OpenAI-based voices)   - **Custom neural voice**    - Professional voice    - Personal voice   - **TTS Avatar**    - Prebuilt avatar    - Custom avatar - **Speech Translation**  - **Description**: Speech Translation enables real-time, multi-language translation of speech, allowing you to add end-to-end, real-time, multi-language translation capabilities to your applications, tools, and devices.  - **Key Features**   - **Realtime Speech Translation**: This Speech service supports real-time, multi-language speech to speech and speech to text translation of audio streams.    - Support both audio and text output    - Automatic language detection    - Integrated customization built-in   - **Video Translation**: This end-to-end solution performs video translation covering global locales.    - End-to-end solution with both no-code and API support    - GPT built-in to optimize the translation content, augmented by content editing    - Personal voice (limited access) to keep the original timbre, emotions, intonation &amp; style intact ## Use cases **Speech To Text** | Use case | Scenario | Solution | | :---: | :--- | :--- | | Live meeting transcriptions and captions | A virtual event platform needs to provide real-time captions for webinars. | Integrate real-time speech to text using the Speech SDK to transcribe spoken content into captions displayed live during the event. | | Customer service enhancement | A call center wants to assist agents by providing real-time transcriptions of customer calls. | Use real-time speech to text via the Speech CLI to transcribe calls, enabling agents to better understand and respond to customer queries. | | Video subtitling | A video-hosting platform wants to quickly generate a set of subtitles for a video. | Use fast transcription to quickly get a set of subtitles for the entire video. | | Educational tools | An e-learning platform aims to provide transcriptions for video lectures. | Apply batch transcription through the speech to text REST API to process prerecorded lecture videos, generating text transcripts for students. | | Healthcare documentation | A healthcare provider needs to document patient consultations. | Use real-time speech to text for dictation, allowing healthcare professionals to speak their notes and have them transcribed instantly. Use a custom model to enhance recognition of specific medical terms. | | Media and entertainment | A media company wants to create subtitles for a large archive of videos. | Use batch transcription to process the video files in bulk, generating accurate subtitles for each video. | | Market research | A market research firm needs to analyze customer feedback from audio recordings. | Employ batch transcription to convert audio feedback into text, enabling easier analysis and insights extraction. | **Text To Speech** | Use case | Scenario | | :---: | :--- | | Educational or interactive learning | To create a fictional brand or character voice for reading or speaking educational materials, online learning, interactive lesson plans, simulation learning, or guided museum tours. | | Media Entertainment | To create a fictional brand or character voice for reading or speaking entertainment content for video games, movies, TV, recorded music, podcasts, audio books, or augmented or virtual reality. | | Media Marketing | To create a fictional brand or character voice for reading or speaking marketing and product or service media, product introductions, business promotion, or advertisements. | | Self-authored content | To create a voice for reading content authored by the voice talent. | | Accessibility Features | For use in audio description systems and narration, including any fictional brand or character voice, or to facilitate communication by people with speech impairments. | | Interactive Voice Response (IVR) Systems | To create voices, including any fictional brand or character voice, for call center operations, telephony systems, or responses for phone interactions. | | Public Service and Informational Announcements | To create a fictional brand or character voice for communicating public service information, including announcements for public venues, or for informational broadcasts such as traffic, weather, event information, and schedules. This use case is not intended for journalistic or news content. | | Translation and Localization | For use in translation applications for translating conversations in different languages or translating audio media. | | Virtual Assistant or Chatbot | To create a fictional brand or character voice for smart assistants in or for virtual web assistants, appliances, cars, home appliances, toys, control of IoT devices, navigation systems, reading out personal messages, virtual companions, or customer service scenarios. | **Speech Translation** | Use case | Scenario | | :---: | :--- | | Realtime translated caption/subtitle | Realtime translated captions /subtitles for meetings or audio/video content | | Realtime audio/video translation (speech-to-speech) | Translate audio/video into target language audio. The input can be short-form videos, live broadcasts, online or in-person conversations (e.g., Live Interpreter), etc. | | Batch Video Translation | Automated dubbing of spoken content in videos from one language to another | ## Benefits **Text To Speech** - **Global Reach with Extensive Locale and Voice Coverage**: Azure TTS supports more than 140 languages and dialects, along with 400+ unique neural voices. Its widespread data center coverage across 60+ Azure regions makes it highly accessible globally, ensuring low-latency voice services in key markets across North America, Europe, Asia Pacific, and emerging markets in Africa, South America, and the Middle East. - **Customization Capabilities**: Azure’s Custom Neural Voice enables businesses to create unique, branded voices in a low/no code self-serving portal that can speak in specific accents or styles, reflecting a company’s identity. This customization extends to creating regional variants and accents, making Azure ideal for multinational corporations seeking to tailor voices to specific local audiences. - **Flexible Deployment Options**: Azure TTS can be deployed in the cloud, on-premises, or at the edge. - **Security and Compliance**: Azure offers end-to-end encryption, comprehensive compliance certifications (like GDPR, HIPAA, ISO 27001, SOC), and a strong focus on privacy. - **TTS Avatar as a Differentiator**: Azure’s TTS avatars, combined with Custom Neural Voice, create immersive, interactive virtual characters. This innovation allows businesses to integrate human-like avatars in customer service, e-learning, and entertainment, providing visually engaging interactions that go beyond simple audio output. **Speech Translation** - **Multiple language detection**: Model will detect multiple languages among the supported languages in the same audio stream. - **Automatic language detection**: No need to specify input languages – model will detect them automatically. - **Integrated custom translation**: Adapt model to your domain-specific vocabulary. - **Simple &amp; Quick**: End-to-end solution that performs video translation covering global locales - **High quality**: GPT built-in to optimize the translation content, augmented by content editing - **Personalized (Limited Access)**: Keep the original timbre, emotions, intonation &amp; style intact ## Pricing Speech is available for many [languages](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/language-support), [regions](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/regions), and [price points](https://azure.microsoft.com/pricing/details/cognitive-services/speech-services/).
        /// </summary>
        [Obsolete("This model is not available anymore.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly AIFoundryModel AzureAiSpeech = new() { Name = "Azure-AI-Speech", Version = "1", Format = "Microsoft" };

        /// <inheritdoc cref="AzureAITranslator"/>
        [Obsolete("Use AzureAITranslator instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly AIFoundryModel AzureAiTranslator = new() { Name = "Azure-AI-Translator", Version = "1", Format = "Microsoft" };

        /// <inheritdoc cref="AzureAIVision"/>
        [Obsolete("Use AzureAIVision instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly AIFoundryModel AzureAiVision = new() { Name = "Azure-AI-Vision", Version = "1", Format = "Microsoft" };

        /// <inheritdoc cref="MaiDSR1"/>
        [Obsolete("Use MaiDSR1 instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly AIFoundryModel MaiDsR1 = new() { Name = "MAI-DS-R1", Version = "1", Format = "Microsoft" };
    }

    /// <inheritdoc cref="MistralAI"/>
    [Obsolete("Use MistralAI instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class MistralAi
    {
        /// <inheritdoc cref="MistralAI.Codestral2501"/>
        public static readonly AIFoundryModel Codestral2501 = new() { Name = "Codestral-2501", Version = "2", Format = "Mistral AI" };

        /// <inheritdoc cref="MistralAI.Codestral2501"/>
        public static readonly AIFoundryModel Ministral3B = new() { Name = "Ministral-3B", Version = "1", Format = "Mistral AI" };

        /// <inheritdoc cref="MistralAI.Codestral2501"/>
        public static readonly AIFoundryModel MistralDocumentAi2505 = new() { Name = "mistral-document-ai-2505", Version = "1", Format = "Mistral AI" };

        /// <inheritdoc cref="MistralAI.Codestral2501"/>
        public static readonly AIFoundryModel MistralLarge2407 = new() { Name = "Mistral-large-2407", Version = "1", Format = "Mistral AI" };

        /// <inheritdoc cref="MistralAI.Codestral2501"/>
        public static readonly AIFoundryModel MistralLarge2411 = new() { Name = "Mistral-Large-2411", Version = "2", Format = "Mistral AI" };

        /// <inheritdoc cref="MistralAI.Codestral2501"/>
        public static readonly AIFoundryModel MistralMedium2505 = new() { Name = "mistral-medium-2505", Version = "1", Format = "Mistral AI" };

        /// <inheritdoc cref="MistralAI.Codestral2501"/>
        public static readonly AIFoundryModel MistralNemo = new() { Name = "Mistral-Nemo", Version = "1", Format = "Mistral AI" };

        /// <inheritdoc cref="MistralAI.Codestral2501"/>
        public static readonly AIFoundryModel MistralSmall = new() { Name = "Mistral-small", Version = "1", Format = "Mistral AI" };

        /// <inheritdoc cref="MistralAI.Codestral2501"/>
        public static readonly AIFoundryModel MistralSmall2503 = new() { Name = "mistral-small-2503", Version = "1", Format = "Mistral AI" };
    }
}
