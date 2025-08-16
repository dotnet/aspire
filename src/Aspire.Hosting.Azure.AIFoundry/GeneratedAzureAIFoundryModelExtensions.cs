// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;

namespace Aspire.Hosting;

/// <summary>
/// Generated extension methods for Azure AI Foundry resources to easily add model deployments.
/// </summary>
public static class GeneratedAzureAIFoundryModelExtensions
{
    // AI21 Labs Models
    /// <summary>
    /// Adds a deployment of AI21 Labs AI21-Jamba-1.5-Large model (A 398B parameters (94B active) multilingual model, offering a 256K long context window, function calling, structured output, and grounded generation.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddAi21Jamba15Large(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "AI21-Jamba-1.5-Large", "1", "AI21 Labs");
    }

    /// <summary>
    /// Adds a deployment of AI21 Labs AI21-Jamba-1.5-Mini model (A 52B parameters (12B active) multilingual model, offering a 256K long context window, function calling, structured output, and grounded generation.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddAi21Jamba15Mini(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "AI21-Jamba-1.5-Mini", "1", "AI21 Labs");
    }

    // Black Forest Labs Models
    /// <summary>
    /// Adds a deployment of Black Forest Labs FLUX-1.1-pro model (Generate images with amazing image quality, prompt adherence, and diversity at blazing fast speeds. FLUX1.1 [pro] delivers six times faster image generation and achieved the highest Elo score on Artificial Analysis benchmarks when launched, surpassing all).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddFlux11Pro(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "FLUX-1.1-pro", "1", "Black Forest Labs");
    }

    /// <summary>
    /// Adds a deployment of Black Forest Labs FLUX.1-Kontext-pro model (Generate and edit images through both text and image prompts. FLUX.1 Kontext is a multimodal flow matching model that enables both text-to-image generation and in-context image editing. Modify images while maintaining character consistency and performing l).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddFlux1KontextPro(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "FLUX.1-Kontext-pro", "1", "Black Forest Labs");
    }

    // Cohere Models
    /// <summary>
    /// Adds a deployment of Cohere cohere-command-a model (Command A is a highly efficient generative model that excels at agentic and multilingual use cases.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddCohereCommandA(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "cohere-command-a", "3", "Cohere");
    }

    /// <summary>
    /// Adds a deployment of Cohere Cohere-command-r model (Command R is a scalable generative model targeting RAG and Tool Use to enable production-scale AI for enterprise.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddCohereCommandR(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Cohere-command-r", "1", "Cohere");
    }

    /// <summary>
    /// Adds a deployment of Cohere Cohere-command-r-08-2024 model (Command R is a scalable generative model targeting RAG and Tool Use to enable production-scale AI for enterprise.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddCohereCommandR082024(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Cohere-command-r-08-2024", "1", "Cohere");
    }

    /// <summary>
    /// Adds a deployment of Cohere Cohere-command-r-plus model (Command R+ is a state-of-the-art RAG-optimized model designed to tackle enterprise-grade workloads.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddCohereCommandRPlus(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Cohere-command-r-plus", "1", "Cohere");
    }

    /// <summary>
    /// Adds a deployment of Cohere Cohere-command-r-plus-08-2024 model (Command R+ is a state-of-the-art RAG-optimized model designed to tackle enterprise-grade workloads.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddCohereCommandRPlus082024(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Cohere-command-r-plus-08-2024", "1", "Cohere");
    }

    /// <summary>
    /// Adds a deployment of Cohere Cohere-embed-v3-english model (Cohere Embed English is the market&apos;s leading text representation model used for semantic search, retrieval-augmented generation (RAG), classification, and clustering.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddCohereEmbedV3English(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Cohere-embed-v3-english", "1", "Cohere");
    }

    /// <summary>
    /// Adds a deployment of Cohere Cohere-embed-v3-multilingual model (Cohere Embed Multilingual is the market&apos;s leading text representation model used for semantic search, retrieval-augmented generation (RAG), classification, and clustering.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddCohereEmbedV3Multilingual(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Cohere-embed-v3-multilingual", "1", "Cohere");
    }

    /// <summary>
    /// Adds a deployment of Cohere embed-v-4-0 model (Embed 4 transforms texts and images into numerical vectors).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddEmbedV40(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "embed-v-4-0", "5", "Cohere");
    }

    // Core42 Models
    /// <summary>
    /// Adds a deployment of Core42 jais-30b-chat model (JAIS 30b Chat is an auto-regressive bilingual LLM for Arabic &amp; English with state-of-the-art capabilities in Arabic.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddJais30bChat(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "jais-30b-chat", "3", "Core42");
    }

    // DeepSeek Models
    /// <summary>
    /// Adds a deployment of DeepSeek DeepSeek-R1 model (DeepSeek-R1 excels at reasoning tasks using a step-by-step training process, such as language, scientific reasoning, and coding tasks.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddDeepseekR1(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "DeepSeek-R1", "1", "DeepSeek");
    }

    /// <summary>
    /// Adds a deployment of DeepSeek DeepSeek-R1-0528 model (The DeepSeek R1 0528 model has improved reasoning capabilities, this version also offers a reduced hallucination rate, enhanced support for function calling, and better experience for vibe coding.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddDeepseekR10528(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "DeepSeek-R1-0528", "1", "DeepSeek");
    }

    /// <summary>
    /// Adds a deployment of DeepSeek DeepSeek-V3 model (A strong Mixture-of-Experts (MoE) language model with 671B total parameters with 37B activated for each token.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddDeepseekV3(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "DeepSeek-V3", "1", "DeepSeek");
    }

    /// <summary>
    /// Adds a deployment of DeepSeek DeepSeek-V3-0324 model (DeepSeek-V3-0324 demonstrates notable improvements over its predecessor, DeepSeek-V3, in several key aspects, including enhanced reasoning, improved function calling, and superior code generation capabilities.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddDeepseekV30324(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "DeepSeek-V3-0324", "1", "DeepSeek");
    }

    // Meta Models
    /// <summary>
    /// Adds a deployment of Meta Llama-3.2-11B-Vision-Instruct model (Excels in image reasoning capabilities on high-res images for visual understanding apps.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddLlama3211BVisionInstruct(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Llama-3.2-11B-Vision-Instruct", "6", "Meta");
    }

    /// <summary>
    /// Adds a deployment of Meta Llama-3.2-90B-Vision-Instruct model (Advanced image reasoning capabilities for visual understanding agentic apps.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddLlama3290BVisionInstruct(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Llama-3.2-90B-Vision-Instruct", "5", "Meta");
    }

    /// <summary>
    /// Adds a deployment of Meta Llama-3.3-70B-Instruct model (Llama 3.3 70B Instruct offers enhanced reasoning, math, and instruction following with performance comparable to Llama 3.1 405B.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddLlama3370BInstruct(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Llama-3.3-70B-Instruct", "7", "Meta");
    }

    /// <summary>
    /// Adds a deployment of Meta Llama-4-Maverick-17B-128E-Instruct-FP8 model (Llama 4 Maverick 17B 128E Instruct FP8 is great at precise image understanding and creative writing, offering high quality at a lower price compared to Llama 3.3 70B).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddLlama4Maverick17B128EInstructFp8(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Llama-4-Maverick-17B-128E-Instruct-FP8", "1", "Meta");
    }

    /// <summary>
    /// Adds a deployment of Meta Llama-4-Scout-17B-16E-Instruct model (Llama 4 Scout 17B 16E Instruct is great at multi-document summarization, parsing extensive user activity for personalized tasks, and reasoning over vast codebases.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddLlama4Scout17B16EInstruct(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Llama-4-Scout-17B-16E-Instruct", "2", "Meta");
    }

    /// <summary>
    /// Adds a deployment of Meta Meta-Llama-3-70B-Instruct model (A powerful 70-billion parameter model excelling in reasoning, coding, and broad language applications.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddMetaLlama370BInstruct(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Meta-Llama-3-70B-Instruct", "9", "Meta");
    }

    /// <summary>
    /// Adds a deployment of Meta Meta-Llama-3-8B-Instruct model (A versatile 8-billion parameter model optimized for dialogue and text generation tasks.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddMetaLlama38BInstruct(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Meta-Llama-3-8B-Instruct", "9", "Meta");
    }

    /// <summary>
    /// Adds a deployment of Meta Meta-Llama-3.1-405B-Instruct model (The Llama 3.1 instruction tuned text only models are optimized for multilingual dialogue use cases and outperform many of the available open source and closed chat models on common industry benchmarks.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddMetaLlama31405BInstruct(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Meta-Llama-3.1-405B-Instruct", "1", "Meta");
    }

    /// <summary>
    /// Adds a deployment of Meta Meta-Llama-3.1-70B-Instruct model (The Llama 3.1 instruction tuned text only models are optimized for multilingual dialogue use cases and outperform many of the available open source and closed chat models on common industry benchmarks.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddMetaLlama3170BInstruct(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Meta-Llama-3.1-70B-Instruct", "4", "Meta");
    }

    /// <summary>
    /// Adds a deployment of Meta Meta-Llama-3.1-8B-Instruct model (The Llama 3.1 instruction tuned text only models are optimized for multilingual dialogue use cases and outperform many of the available open source and closed chat models on common industry benchmarks.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddMetaLlama318BInstruct(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Meta-Llama-3.1-8B-Instruct", "5", "Meta");
    }

    // Microsoft Models
    /// <summary>
    /// Adds a deployment of Microsoft Azure-AI-Content-Safety model (## Azure AI Content Safety ## Introduction Azure AI Content Safety is a safety system for monitoring content generated by both foundation models and humans. Detect and block potential risks, threats, and quality problems. You can build an advanced safety system for foundation models to detect and mitigate harmful content and risks in user prompts and AI-generated outputs. Use Prompt Shields to detect and block prompt injection attacks, groundedness detection to pinpoint ungrounded or hallucinated materials, and protected material detection to identify copyrighted or owned content. ## Core Features - **Block harmful input and output**  - **Description**: Detect and block violence, hate, sexual, and self-harm content for both text, images and multimodal. Configure severity thresholds for your specific use case and adhere to your responsible AI policies.  - **Key Features**: Violence, hate, sexual, and self-harm content detection. Custom blocklist. - **Policy customization with custom categories**  - **Description**: Create unique content filters tailored to your requirements using custom categories. Quickly train a new custom category by providing examples of content you need to block.  - **Key Features**: Custom categories - **Identify the security risks**  - **Description**: Safeguard your AI applications against prompt injection attacks and jailbreak attempts. Identify and mitigate both direct and indirect threats with prompt shields.  - **Key Features**: Direct jailbreak attack, indirect prompt injection from docs. - **Detect and correct Gen AI hallucinations**  - **Description**: Identify and correct generative AI hallucinations and ensure outputs are reliable, accurate, and grounded in data with groundedness detection.  - **Key Features**: Groundedness detection, reasoning, and correction. - **Identify protected material**  - **Description**: Pinpoint copyrighted content and provide sources for preexisting text and code with protected material detection.  - **Key Features**: Protected material for code, protected material for text ## Use Cases - Generative AI services screen user-submitted prompts and generated outputs to ensure safe and appropriate content. - Online marketplaces monitor and filter product listings and other user-generated content to prevent harmful or inappropriate material. - Gaming platforms manage and moderate user-created game content and in-game communication to maintain a safe environment. - Social media platforms review and regulate user-uploaded images and posts to enforce community standards and prevent harmful content. - Enterprise media companies implement centralized content moderation systems to ensure the safety and appropriateness of their published materials. - K-12 educational technology providers filter out potentially harmful or inappropriate content to create a safe learning environment for students and educators. ## Benefits - **No ML experience required**: Incorporate content safety features into your projects with no machine learning experience required. - **Effortlessly customize your RAI policies**: Customizing your content safety classifiers can be done with one line of description, a few samples using Custom Categories. - **State of the art models**: ready for use APIs, SOTA models, and flexible deployment options reduce the need for ongoing manual training or extensive customization. Microsoft has a science team and policy experts working on the frontier of Gen AI to constantly improve the safety and security models to ensure our customers can develop and deploy generative AI safely and responsibly. - **Global Reach**: Support more than 100 languages, enabling businesses to communicate effectively with customers, partners, and employees worldwide. - **Scalable and Reliable**: Built on Azure’s cloud infrastructure, the Azure AI Content Safety service scales automatically to meet demand, from small business applications to global enterprise workloads. - **Security and Compliance**: Azure AI Content Safety runs on Azure’s secure cloud infrastructure, ensuring data privacy and compliance with global standards. User data is not stored after the translation process. - **Flexible deployment**: Azure AI Content Safety can be deployed on cloud, on premises and on devices. ## Technical Details - **Deployment**  - **Container for on-premise deployment**: [Content safety containers overview - Azure AI Content Safety - Azure AI services | Microsoft Learn](https://learn.microsoft.com/azure/ai-services/content-safety/how-to/containers/container-overview)  - **Embedded Content Safety**: [Embedded Content Safety - Azure AI Content Safety - Azure AI services | Microsoft Learn](https://learn.microsoft.com/azure/ai-services/content-safety/how-to/embedded-content-safety?tabs=windows-target%2Ctext)  - **Cloud**: [Azure AI Content Safety documentation - Quickstarts, Tutorials, API Reference - Azure AI services | Microsoft Learn](https://learn.microsoft.com/azure/ai-services/content-safety/) - **Requirements**: Requirements vary feature by feature, for more details, refer to the Azure AI Content Safety documentation: [Azure AI Content Safety documentation - Quickstarts, Tutorials, API Reference - Azure AI services | Microsoft Learn](https://learn.microsoft.com/azure/ai-services/content-safety/). - **Support**: Azure AI Content Safety is part of Azure AI Services. Support options for AI Services can be found here: [Azure AI services support and help options - Azure AI services | Microsoft Learn](https://learn.microsoft.com/azure/ai-services/cognitive-services-support-options?context=%2Fazure%2Fai-services%2Fcontent-safety%2Fcontext%2Fcontext). ## Pricing Explore pricing options here: [Azure AI Content Safety - Pricing | Microsoft Azure](https://azure.microsoft.com/pricing/details/cognitive-services/content-safety/).).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddAzureAiContentSafety(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Azure-AI-Content-Safety", "1", "Microsoft");
    }

    /// <summary>
    /// Adds a deployment of Microsoft Azure-AI-Content-Understanding model (# Azure AI Content Understanding  ## Introduction Azure AI Content Understanding empowers you to transform unstructured multimodal data—such as text, images, audio, and video—into structured, actionable insights. By streamlining content processing with advanced AI techniques like schema extraction and grounding, it delivers accurate structured data for downstream applications. Offering prebuilt templates for common use cases and customizable models, it helps you unify diverse data types into a single, efficient pipeline, optimizing workflows and accelerating time to value. ## Core Features  - **Multimodal data ingestion**  Ingest a range of modalities such as documents, images, audio, or video. Use a variety of AI models to convert the input data into a structured format that can be easily processed and analyzed by downstream services or applications. - **Customizable output schemas**  Customize the schemas of extracted results to meet your specific needs. Tailor the format and structure of summaries, insights, or features to include only the most relevant details—such as key points or timestamps—from video or audio files.  - **Confidence scores** Leverage confidence scores to minimize human intervention and continuously improve accuracy through user feedback. - **Output ready for downstream applications**  Automate business processes by building enterprise AI apps or agentic workflows. Use outputs that downstream applications can consume for reasoning with retrieval-augmented generation (RAG). - **Grounding** Ensure the information extracted, inferred, or abstracted is represented in the underlying content. - **Automatic labeling** Save time and effort on manual annotation and create models quicker by using large language models (LLMs) to extract fields from various document types. ## Use Cases  - **Post-call analytics for call centers**: Generate insights from call recordings, track key performance indicators (KPIs), and answer customer questions more accurately and efficiently.  - **Tax process automation**: Streamline the tax return process by extracting data from tax forms to create a consolidated view of information across various documents.  - **Media asset management**: Extract features from images and videos to provide richer tools for targeted content and enhance media asset management solutions.  - **Chart understanding**: Enhance chart understanding by automating the analysis and interpretation of various types of charts and diagrams using Content Understanding. ## Benefits  - **Streamline workflows**: Azure AI Content Understanding standardizes the extraction of content, structure, and insights from various content types into a unified process. - **Simplify field extraction**: Field extraction in Content Understanding makes it easier to generate structured output from unstructured content. Define a schema to extract, classify, or generate field values with no complex prompt engineering. - **Enhance accuracy**: Content Understanding employs multiple AI models to analyze and cross-validate information simultaneously, resulting in more accurate and reliable results. - **Confidence scores &amp; grounding**: Content Understanding ensures the accuracy of extracted values while minimizing the cost of human review. ## Technical Details  - **Deployment**: Deployment options may vary by service, reference the following docs for more information: [Create an Azure AI Services multi-service resource](https://learn.microsoft.com/en-us/azure/ai-services/content-understanding/how-to/create-multi-service-resource). - **Requirements**: Requirements may vary depending on the input data you are analyzing, reference the following docs for more information: [Service quotas and limits](https://learn.microsoft.com/en-us/azure/ai-services/content-understanding/service-limits). - **Support**: Support options for AI Services can be found here: [Azure AI services support and help options](https://learn.microsoft.com/en-us/azure/ai-services/cognitive-services-support-options?view=doc-intel-4.0.0).  ## Pricing  View up-to-date pay-as-you-go pricing details here: [Azure AI Content Understanding pricing](https://azure.microsoft.com/en-us/pricing/details/content-understanding/).).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddAzureAiContentUnderstanding(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Azure-AI-Content-Understanding", "1", "Microsoft");
    }

    /// <summary>
    /// Adds a deployment of Microsoft Azure-AI-Document-Intelligence model (## Azure AI Document Intelligence Document Intelligence is a cloud-based service that enables you to build intelligent document processing solutions. Massive amounts of data, spanning a wide variety of data types, are stored in forms and documents. Document Intelligence enables you to effectively manage the velocity at which data is collected and processed and is key to improved operations, informed data-driven decisions, and enlightened innovation.  ## Core Features - **General extraction models**  - **Description**: General extraction models enable text extraction from forms and documents and return structured business-ready content ready for your organization&apos;s action, use, or development.  - **Key Features**   - Read model allows you to extract written or printed text liens, words, locations, and detected languages.   - Layout model, on top of text extraction, extracts structural information like tables, selection marks, paragraphs, titles, headings, and subheadings. Layout model can also output the extraction results in a Markdown format, enabling you to define your semantic chunking strategy based on provided building blocks, allowing for easier RAG (Retrieval Augmented Generation). - **Prebuilt models**  - **Description**: Prebuilt models enable you to add intelligent document processing to your apps and flows without having to train and build your own models. Prebuilt models extract a pre-defined set of fields depending on the document type.  - **Key Features**   - **Financial Services and Legal Documents**: Credit Cards, Bank Statement, Pay Slip, Check, Invoices, Receipts, Contracts.   - **US Tax Documents**: Unified Tax, W-2, 1099 Combo, 1040 (multiple variations), 1098 (multiple variations), 1099 (multiple variations).   - **US Mortgage Documents**: 1003, 1004, 1005, 1008, Closing Disclosure.   - **Personal Identification Documents**: Identity Documents, Health Insurance Cards, Marriage Certificates. - **Custom models**  - **Description**: Custom models are trained using your labeled datasets to extract distinct data from forms and documents, specific to your use cases. Standalone custom models can be combined to create composed models.  - **Key Features**   - **Document field extraction models**    - **Custom generative**: Build a custom extraction model using generative AI for documents with unstructured format and varying templates.    - **Custom neural**: Extract data from mixed-type documents.    - **Custom template**: Extract data from static layouts.    - **Custom composed**: Extract data using a collection of models. Explicitly choose the classifier and enable confidence-based routing based on the threshold you set.   - **Custom classification models**    - **Custom classifier**: Identify designated document types (classes) before invoking an extraction model. - **Add-on capabilities**  - **Description**: Use the add-on features to extend the results to include more features extracted from your documents. Some add-on features incur an extra cost. These optional features can be enabled and disabled depending on the scenario of the document extraction.  - **Key Features**   - High resolution extraction   - Formula extraction   - Font extraction   - Barcode extraction   - Language detection   - Searchable PDF output ## Use Cases - **Accounts payable**: A company can increase the efficiency of its accounts payable clerks by using the prebuilt invoice model and custom forms to speed up invoice data entry with a human in the loop. The prebuilt invoice model can extract key fields, such as Invoice Total and Shipping Address. - **Insurance form processing**: A customer can train a model by using custom forms to extract a key-value pair in insurance forms and then feeds the data to their business flow to improve the accuracy and efficiency of their process. For their unique forms, customers can build their own model that extracts key values by using custom forms. These extracted values then become actionable data for various workflows within their business. - **Bank form processing**: A bank can use the prebuilt ID model and custom forms to speed up the data entry for &quot;know your customer&quot; documentation, or to speed up data entry for a mortgage packet. If a bank requires their customers to submit personal identification as part of a process, the prebuilt ID model can extract key values, such as Name and Document Number, speeding up the overall time for data entry. - **Robotic process automation (RPA)**: Using the custom extraction model, customers can extract specific data needed from distinct types of documents. The key-value pair extracted can then be entered into various systems such as databases, or CRM systems, through RPA, replacing manual data entry. Customers can also use custom classification model to categorize documents based on their content and file them in proper location. As such, an organized set of data extracted from the custom model can be an essential first step to document RPA scenarios for businesses that manage large volumes of documents regularly. ## Benefits - **No experience required**: Incorporate Document Intelligence features into your projects with no machine learning experience required. - **Effortlessly customize your models**: Training your own custom extraction and classification model can be done with as little as one document labeled, making it easy to train your own models. - **State of the art models**: ready for use APIs, constantly enhanced models, and flexible deployment options reduce the need for ongoing manual training or extensive customization. ## Technical Details: - **Deployment**: Deployment options may vary by service, reference the following docs for more information: [Use Document Intelligence models](https://learn.microsoft.com/azure/ai-services/document-intelligence/how-to-guides/use-sdk-rest-api?view=doc-intel-3.1.0&amp;tabs=linux&amp;pivots=programming-language-rest-api) and [Install and run containers](https://learn.microsoft.com/azure/ai-services/document-intelligence/containers/install-run?view=doc-intel-4.0.0&amp;tabs=read). - **Requirements**: Requirements may vary slightly depending on the model you are using to analyze the documents. Reference the following docs for more information: [Service quotas and limits](https://learn.microsoft.com/azure/ai-services/document-intelligence/service-limits?view=doc-intel-4.0.0). - **Support**: Support options for AI Services can be found here: [Azure AI services support and help options - Azure AI services | Microsoft Learn](https://learn.microsoft.com/azure/ai-services/cognitive-services-support-options?context=%2Fazure%2Fai-services%2Fdocument-intelligence%2Fcontext%2Fcontext&amp;view=doc-intel-4.0.0). ## Pricing View up-to-date pricing information for the pay-as-you-go pricing model here: [Azure AI Document Intelligence pricing](https://azure.microsoft.com/pricing/details/ai-document-intelligence/).).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddAzureAiDocumentIntelligence(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Azure-AI-Document-Intelligence", "1", "Microsoft");
    }

    /// <summary>
    /// Adds a deployment of Microsoft Azure-AI-Language model (## Azure AI Language Azure AI Language is a cloud-based service designed to help you easily get insights from unstructured text data. It uses a combination of SLMs and LLMs, including task-optimized decoder models and encoder models, for Language AI solutions. It provides premium quality at an affordable price, excels in scale and low latency. With it, you can extract, classify, and summarize information to gain insights. You can also customize and finetune them for your specific needs. It empowers you to integrate natural language into apps, bots, and IoT devices. For example, it can redact sensitive data, segment long meetings into chapters, analyze health records, and orchestrate conversational bots on your custom intents and factual answers to ensure consistency and control. ## Core Features - **Extract Classify and Understand Information**  - **Description**: Extract and distill key insights from unstructured data, such as named entities, medical information, important statements, etc. and analyze sentiment and my opinion.  - **Key Features**: Named Entity Recognition (NER), Custom Extraction, Key Phrase Extraction, Health Information Extraction, Text Summarization, Extractive summarization, Abstractive summarization, Sentiment Analysis, Language Detection. - **Enhanced Conversational Experiences**  - **Description**: Customize your conversational experience with a deterministic and repeatable solution; distill insights from long conversion, empower intelligent conversational agents that can understand, respond, and orchestrate responses in a natural, context-aware manner  - **Key Features**: Conversation Summarization, Conversational Language Understanding (CLU), Question Answering (Q&amp;A), and Orchestration Workflow - **Data Privacy and Compliance**  - **Description**: Identify personally identifiable information, masking it as needed to help you to adhere to your privacy policies.  - **Key Features**: PII Detection, PII Redaction. ## Use Cases - **Protect privacy data with PII detection**: Use PII detection to identify and redact sensitive information before sending your data to LLMs or other cloud services. Redact personal information to protect your customers’ privacy from call center transcription, reduce unconscious bias from resumes, apply sensitivity labels for documents, or clean your data and reduce unfairness for data science. - **Reduce hallucinations and derive insights with Name Entity Recognition and Text Analytics for health**: Use Named Entity Recognition or Text Analytics for health to reduce hallucinations from LLMs by prompting the model with extracted entity values (e.g., product names, price numbers, MedDRA code, etc.). Build knowledge graphs based on entities detected in documents to enhance search quality. Extract key information to enable business process automation. Derive insights into popular information from customer reviews, emails, and calls. - **Meeting Summarization for Efficient Recaps and Chaptering**: Using summarization features, long meetings can be effectively condensed into quick recaps and organized into timestamped chapters with detailed narratives, making the information more accessible to both participants and those who missed the meeting. - **Call Center Summarization**: Using summarization features, customer service calls can be efficiently summarized into concise recaps with focused notes on customer issues and the resolutions provided by agents. This allows agents and supervisors to quickly review key details, improving follow-up actions and overall customer satisfaction. - **Build deterministic and repeatable conversational AI experience**: Use conversational language understanding (CLU) to define the top user intents and key information you want to track over the conversations. Build your Q&amp;A bot with custom question answering to control the wording in answers for critical questions with hallucination worry-free. Route user queries over orchestration workflow based on users’ intents or questions. - **Analyze healthcare data with Text Analytics for health**: Use Text Analytics for health to extract insights and statistics, develop predictive models and flag possible errors from clinical notes, research documents and medical reports by identifying medical entities, entity relationships and assertions. Auto-annotate and curate clinical data such as automating clinical coding and digitizing manually created data by using entity linking to Unified Medical Language System (UMLS) Metathesaurus and other Text Analytics for health features. ## Benefits - **Premium Quality**: Pre-trained task-optimized models ensure premium quality as they are built on vast, diverse datasets and fine-tuned by experts to deliver accurate and reliable results across various use cases - **Low Maintenance**: Ready to use APIs, constantly enhanced models, and flexible deployment options reduce the need for ongoing prompt rewriting, manual training, or extensive customization. This allows you to focus business insights rather than managing infrastructure. - **Enterprise Scalability**: Scalable across multiple environments, from on-premises containers to cloud-based services. adaptable to different workflows and data volumes without sacrificing performance. seamlessly integrated into various enterprise systems ## Technical Details - **Deployment**: Azure AI Language is composed of many natural language process capabilities. All are available as cloud and most of them also have container offerings. - **Requirements**: Azure AI Language requirements may vary slightly depending on the model you are using. Reference the following docs for more information: [Data limits for Language service features - Azure AI services | Microsoft Learn](https://learn.microsoft.com/azure/ai-services/language-service/concepts/data-limits). - **Support**: Azure AI Language is part of Azure AI Services. Support options for AI Services can be found here: [Azure AI services support and help options](https://learn.microsoft.com/azure/ai-services/cognitive-services-support-options?context=%2Fazure%2Fai-services%2Fcomputer-vision%2Fcontext%2Fcontext). ## Pricing Azure AI Language offers competitive pricing. The pricing model includes pay-as-go and discounts based on volume commitments. Explore [Azure AI Language pricing options here](https://aka.ms/languageprice).).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddAzureAiLanguage(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Azure-AI-Language", "1", "Microsoft");
    }

    /// <summary>
    /// Adds a deployment of Microsoft Azure-AI-Speech model (## Azure AI Speech ## Introduction The Speech service provides speech to text and text to speech capabilities with a Speech resource. You can transcribe speech to text with high accuracy, produce natural-sounding text to speech voices, translate spoken audio, and use speaker recognition during conversations. Create custom voices, add specific words to your base vocabulary, or build your own models. Run Speech anywhere, in the cloud or at the edge in containers. It&apos;s easy to speech enable your applications, tools, and devices with the [Speech CLI](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/spx-overview), [Speech SDK](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/speech-sdk), and [REST APIs](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/rest-speech-to-text). ## Core Features - **Speech To Text**  - **Description**: Use speech to text to transcribe audio into text, either in real-time or asynchronously with batch transcription. Convert audio to text from a range of sources, including microphones, audio files, and blob storage. Use speaker diarization to determine who said what and when. Get readable transcripts with automatic formatting and punctuation.   The base model might not be sufficient if the audio contains ambient noise or includes numerous industry and domain-specific jargon. In these cases, you can create and train custom speech models with acoustic, language, and pronunciation data. Custom speech models are private and can offer a competitive advantage.  - **Key Features**   - Real Time Speech To Text   - Transcriptions, captions, or subtitles for live meetings   - [Diarization](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/get-started-stt-diarization)   - [Pronunciation assessment](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/how-to-pronunciation-assessment)   - Contact center agents assist   - Dictation   - Voice agents   - Fast Transcription   - Quick audio or video transcription, subtitles, and edit   - Video translation   - Batch Transcription   - Transcriptions, captions, or subtitles for prerecorded audio   - Contact center post-call analytics   - Diarization   - Custom Speech   - Models with enhanced accuracy for specific domains and conditions. - **Text To Speech**  - **Description**: With [text to speech](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/text-to-speech), you can convert input text into human like synthesized speech. Use human-like prebuilt neural voices out of the box in more than 140 locales and 500 voices or create a custom neural voice that&apos;s unique to your product or brand. You can also enhance the voice experience by using together with Text to speech Avatar to convert text to life-like and high-quality synthetic talking avatar videos.   - **Prebuilt neural voice**: Highly natural out-of-the-box voices. Check the prebuilt neural voice samples the [Voice Gallery](https://speech.microsoft.com/portal/voicegallery) and determine the right voice for your business needs.   - **Custom neural voice**: Besides the prebuilt neural voices that come out of the box, you can also create a [custom neural voice](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/custom-neural-voice) that is recognizable and unique to your brand or product. Custom neural voices are private and can offer a competitive advantage. Check the custom neural voice samples [here](https://aka.ms/customvoice).   - **Text to speech Avatar**: You can convert text into a digital video of a photorealistic human (either a prebuilt avatar or a [custom text to speech avatar](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/text-to-speech-avatar/what-is-text-to-speech-avatar#custom-text-to-speech-avatar)) speaking with a natural-sounding voice. It works best with the Azure neural voices.  - **Key Features**   - **Prebuilt neural voice**    - Neural voice (incl. OpenAI-based voices)    - Neural HD voice (incl. OpenAI-based voices)   - **Custom neural voice**    - Professional voice    - Personal voice   - **TTS Avatar**    - Prebuilt avatar    - Custom avatar - **Speech Translation**  - **Description**: Speech Translation enables real-time, multi-language translation of speech, allowing you to add end-to-end, real-time, multi-language translation capabilities to your applications, tools, and devices.  - **Key Features**   - **Realtime Speech Translation**: This Speech service supports real-time, multi-language speech to speech and speech to text translation of audio streams.    - Support both audio and text output    - Automatic language detection    - Integrated customization built-in   - **Video Translation**: This end-to-end solution performs video translation covering global locales.    - End-to-end solution with both no-code and API support    - GPT built-in to optimize the translation content, augmented by content editing    - Personal voice (limited access) to keep the original timbre, emotions, intonation &amp; style intact ## Use cases **Speech To Text** | Use case | Scenario | Solution | | :---: | :--- | :--- | | Live meeting transcriptions and captions | A virtual event platform needs to provide real-time captions for webinars. | Integrate real-time speech to text using the Speech SDK to transcribe spoken content into captions displayed live during the event. | | Customer service enhancement | A call center wants to assist agents by providing real-time transcriptions of customer calls. | Use real-time speech to text via the Speech CLI to transcribe calls, enabling agents to better understand and respond to customer queries. | | Video subtitling | A video-hosting platform wants to quickly generate a set of subtitles for a video. | Use fast transcription to quickly get a set of subtitles for the entire video. | | Educational tools | An e-learning platform aims to provide transcriptions for video lectures. | Apply batch transcription through the speech to text REST API to process prerecorded lecture videos, generating text transcripts for students. | | Healthcare documentation | A healthcare provider needs to document patient consultations. | Use real-time speech to text for dictation, allowing healthcare professionals to speak their notes and have them transcribed instantly. Use a custom model to enhance recognition of specific medical terms. | | Media and entertainment | A media company wants to create subtitles for a large archive of videos. | Use batch transcription to process the video files in bulk, generating accurate subtitles for each video. | | Market research | A market research firm needs to analyze customer feedback from audio recordings. | Employ batch transcription to convert audio feedback into text, enabling easier analysis and insights extraction. | **Text To Speech** | Use case | Scenario | | :---: | :--- | | Educational or interactive learning | To create a fictional brand or character voice for reading or speaking educational materials, online learning, interactive lesson plans, simulation learning, or guided museum tours. | | Media Entertainment | To create a fictional brand or character voice for reading or speaking entertainment content for video games, movies, TV, recorded music, podcasts, audio books, or augmented or virtual reality. | | Media Marketing | To create a fictional brand or character voice for reading or speaking marketing and product or service media, product introductions, business promotion, or advertisements. | | Self-authored content | To create a voice for reading content authored by the voice talent. | | Accessibility Features | For use in audio description systems and narration, including any fictional brand or character voice, or to facilitate communication by people with speech impairments. | | Interactive Voice Response (IVR) Systems | To create voices, including any fictional brand or character voice, for call center operations, telephony systems, or responses for phone interactions. | | Public Service and Informational Announcements | To create a fictional brand or character voice for communicating public service information, including announcements for public venues, or for informational broadcasts such as traffic, weather, event information, and schedules. This use case is not intended for journalistic or news content. | | Translation and Localization | For use in translation applications for translating conversations in different languages or translating audio media. | | Virtual Assistant or Chatbot | To create a fictional brand or character voice for smart assistants in or for virtual web assistants, appliances, cars, home appliances, toys, control of IoT devices, navigation systems, reading out personal messages, virtual companions, or customer service scenarios. | **Speech Translation** | Use case | Scenario | | :---: | :--- | | Realtime translated caption/subtitle | Realtime translated captions /subtitles for meetings or audio/video content | | Realtime audio/video translation (speech-to-speech) | Translate audio/video into target language audio. The input can be short-form videos, live broadcasts, online or in-person conversations (e.g., Live Interpreter), etc. | | Batch Video Translation | Automated dubbing of spoken content in videos from one language to another | ## Benefits **Text To Speech** - **Global Reach with Extensive Locale and Voice Coverage**: Azure TTS supports more than 140 languages and dialects, along with 400+ unique neural voices. Its widespread data center coverage across 60+ Azure regions makes it highly accessible globally, ensuring low-latency voice services in key markets across North America, Europe, Asia Pacific, and emerging markets in Africa, South America, and the Middle East. - **Customization Capabilities**: Azure’s Custom Neural Voice enables businesses to create unique, branded voices in a low/no code self-serving portal that can speak in specific accents or styles, reflecting a company’s identity. This customization extends to creating regional variants and accents, making Azure ideal for multinational corporations seeking to tailor voices to specific local audiences. - **Flexible Deployment Options**: Azure TTS can be deployed in the cloud, on-premises, or at the edge. - **Security and Compliance**: Azure offers end-to-end encryption, comprehensive compliance certifications (like GDPR, HIPAA, ISO 27001, SOC), and a strong focus on privacy. - **TTS Avatar as a Differentiator**: Azure’s TTS avatars, combined with Custom Neural Voice, create immersive, interactive virtual characters. This innovation allows businesses to integrate human-like avatars in customer service, e-learning, and entertainment, providing visually engaging interactions that go beyond simple audio output. **Speech Translation** - **Multiple language detection**: Model will detect multiple languages among the supported languages in the same audio stream. - **Automatic language detection**: No need to specify input languages – model will detect them automatically. - **Integrated custom translation**: Adapt model to your domain-specific vocabulary. - **Simple &amp; Quick**: End-to-end solution that performs video translation covering global locales - **High quality**: GPT built-in to optimize the translation content, augmented by content editing - **Personalized (Limited Access)**: Keep the original timbre, emotions, intonation &amp; style intact ## Pricing Speech is available for many [languages](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/language-support), [regions](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/regions), and [price points](https://azure.microsoft.com/pricing/details/cognitive-services/speech-services/).).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddAzureAiSpeech(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Azure-AI-Speech", "1", "Microsoft");
    }

    /// <summary>
    /// Adds a deployment of Microsoft Azure-AI-Translator model (## Azure AI Translator Azure AI Translator, a part of the Azure AI services, is a cloud-based neural machine translation service that enables businesses to translate text and documents across multiple languages in real time and in batches. The service also offers customization options, enabling businesses to fine-tune translations to specific domain or industry contexts. Azure AI Translator supports more than 100 languages and dialects, and it scales automatically to handle both small-scale projects and enterprise-level translation needs.   Azure AI Translator powers many Microsoft products and services used by thousands of businesses and millions of users worldwide for language translation and other language-related operations. ## Core Features - **Text Translation**  - **Description**: Translates text across multiple languages in real time, making it easy to integrate translation into data process automation, facilitate conversation between speakers of different languages, perform live caption translation, and browse webpages in the language of your choice.  - **Key Features**   - **Translate**: Translates a single text phrase or an array of text phrases to multiple target-language texts. Users can specify whether to use standard or custom machine translation models in the request.   - **Transliterate**: Converts a single text phrase or an array of text phrases from native script to Latin script and vice versa.   - **Languages**: Returns a list of languages supported by Translate and Transliterate operations. This request does not require authentication. - **Document Translation**  - **Description**: Translates complex documents across all supported languages and dialects while preserving original document structure and data format. Documents can be translated using standard or custom machine translation models, with the option for users to provide glossaries to ensure that specific terms are translated consistently according to their preferences.  - **Key Features**   - **Batch translation**: Translates multiple documents and large files asynchronously across up to 10 target languages in a single request. The service retrieves source documents from an Azure blob storage container, processes and translates the textual content, and then places the translated documents into a target Azure blob storage container.   - **Single document translation**: Translates a small single document into one target language. It accepts the document as part of the request, processes and translates the textual content, and returns the translated document as part of the response. - **Custom Translator**  - **Description**: Custom Translator is a feature of the Azure AI Translator service that enables enterprises, app developers, and language service providers to build customized neural machine translation (NMT) systems.  - **Key Features**   - **Customize with parallel data**: Build translation systems using parallel documents that understand the terminologies used in your own business and industry.   - **Customize with dictionary data**: Build translation systems using bilingual dictionaries of terms used in your own business and industry. ## Use Cases - **Webpage translation**: Translate webpages to engage global audiences in their native language. - **Conversation translation**: Break communication barriers by enabling live multi-lingual conversations in chat applications, customer support, and conferencing tools by providing real-time text and speech translation. - **Document translation**: Translate manuals, marketing materials, documentation, product or service descriptions, specifications, instructions, contracts, etc. to execute business operations across the world. - **Accessibility**: Translate live captions in a TV program or an event for user to follow in their native language. - **Education**: Learn or teach a foreign language with ease. Facilitate cross-language communication in educational settings, enabling students, teachers, and parents to interact seamlessly in multiple languages. - **Social &amp; entertainment**: Engage with people worldwide on social media in your native language, learning new topics and sharing your thoughts. Join online gaming chats with players from different countries and watch movies or programs in foreign languages with subtitle translation. - **Digital investigation**: Translate business intelligence content into a target language for consumption and analysis. ## Benefits - **Global Reach**: Translate content into over 100 languages, enabling businesses to communicate effectively with customers, partners, and employees worldwide. - **Scalable and Reliable**: Built on Azure’s cloud infrastructure, the Translator service automatically scales to meet demand, from small business applications to global enterprise workloads. - **Security and Compliance**: Azure AI Translator runs on Azure’s secure cloud infrastructure, ensuring data privacy and compliance with global standards. User data is not stored after the translation process. - **Customization**: Customize translation models to suit your business-specific terminology and style, improving the accuracy of translations for specialized industries such as legal, medical, or technical fields. - **Easy Integration**: Azure AI Translator service can be easily integrated into various applications through REST APIs and SDKs, making it accessible for developers across platforms. - **Availability**: Azure AI Translator supports translation across over 100 languages. Enables translation of content in languages, for which native speakers and human translators are not available to you. - **Time**: Translate content within seconds, minutes, or a few hours, which otherwise takes several days with human translation. Enables adoption of translation in workflow automation and real-time conversations. - **Cost**: Translate large volumes of content at a fraction of the cost—up to 1,000 times less than traditional human translation—making it accessible even when high costs would normally be a barrier.  ## Technical Details - **Deployment**: Azure AI Translator is available both as cloud and container offering. Translator container offering is gated and is available in [connected (for billing only)](https://learn.microsoft.com/azure/ai-services/translator/containers/overview#connected-containers) and [disconnected (for air gapped network)](https://learn.microsoft.com/azure/ai-services/translator/containers/overview#disconnected-containers). - **Requirements**: Azure AI Translator prerequisites differ based on the core features and the deployment environment. Please refer to the document for more information – [Text Translation Overview](https://aka.ms/TranslatorText), [Document Translation Overview](https://aka.ms/DocumentTranslationDocs), [Translator Container Overview](https://aka.ms/TranslatorContainerDocs), and [Custom Translator Overview](https://aka.ms/CustomTranslator) for specific requirements. - **Support**: Azure AI Translator is part of Azure AI Services. Support options for AI Services can be found here: [Azure AI services support and help options](https://learn.microsoft.com/azure/ai-services/cognitive-services-support-options?context=%2Fazure%2Fai-services%2Fcomputer-vision%2Fcontext%2Fcontext). ## Pricing Azure AI Translator offers competitive pricing. The pricing model includes pay-as-go and discounts based on volume commitments. Explore [Azure AI Translator pricing options here](https://aka.ms/TranslatorPricing).).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddAzureAiTranslator(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Azure-AI-Translator", "1", "Microsoft");
    }

    /// <summary>
    /// Adds a deployment of Microsoft Azure-AI-Vision model (## Azure AI Vision ## Introduction The Azure AI Vision service gives you access to advanced algorithms that process images and videos and return insights based on the visual features and content you are interested in. Azure AI Vision can power a diverse set of scenarios, including digital asset management, video content search &amp; summary, identity verification, generating accessible alt-text for images, and many more. The key product categories for Azure AI Vision include Video Analysis, Image Analysis, Face, and Optical Character Recognition.  ## Core Features - **Video analysis**  - **Description**: Video Analysis includes video-related features like Spatial Analysis and Video Retrieval. Spatial Analysis analyzes the presence and movement of people on a video feed and produces events that other systems can respond to. Video Retrieval lets you create an index of videos that you can search in your natural language.  - **Key Features**: Video retrieval, spatial analysis, person counting, person in a zone, person crossing a line, person distance - **Face**  - **Description**: The Face service provides AI algorithms that detect, recognize, and analyze human faces in images. Facial recognition software is important in many different scenarios, such as identification, touchless access control, and face blurring for privacy.  - **Key Features**: Face detection and analysis, face liveness, face identification, face verification - **Image analysis**  - **Description**: The Image Analysis service extracts many visual features from images, such as objects, faces, adult content, and auto-generated text descriptions.  - **Key Features**: Image tagging, image classification, object detection, image captioning, dense captioning, face detection, optical character recognition, image embeddings, and image search - **Optical character recognition**  - **Description**: The Optical Character Recognition (OCR) service extracts text from images. You can use the Read API to extract printed and handwritten text from photos and documents. It uses deep-learning-based models and works with text on various surfaces and backgrounds. These include business documents, invoices, receipts, posters, business cards, letters, and whiteboards. The OCR APIs support extracting printed text in several languages.  - **Key Features**: OCR ## Use Cases - Boost content discovery with image analysis - Verify identities with the Face service - Search content in videos ## Benefits - **No experience required**: Incorporate vision features into your projects with no machine learning experience required. - **Effortlessly customize your models**: Customizing your image classification and object detection models can be done with as little as one image per tag, making it easy to train your own models. - **State of the art models**: Ready to use APIs, constantly enhanced models, and flexible deployment options reduce the need for ongoing manual training or extensive customization. ## Technical Details - **Deployment**: Deployment options may vary by service, reference the following docs for more information: [Image Analysis Overview](https://learn.microsoft.com/azure/ai-services/computer-vision/overview-image-analysis?tabs=4-0), [Optical Character Recognition Overview](https://learn.microsoft.com/azure/ai-services/computer-vision/overview-ocr), [Video Analysis Overview](https://learn.microsoft.com/azure/ai-services/computer-vision/intro-to-spatial-analysis-public-preview?tabs=sa), and [Face Overview](https://learn.microsoft.com/azure/ai-services/computer-vision/overview-identity). - **Requirements**: Requirements may very slightly depending on the data you are analyzing, reference the following docs for more information: [Image Analysis Overview](https://learn.microsoft.com/azure/ai-services/computer-vision/overview-image-analysis?tabs=4-0), [Optical Character Recognition Overview](https://learn.microsoft.com/azure/ai-services/computer-vision/overview-ocr), [Video Analysis Overview](https://learn.microsoft.com/azure/ai-services/computer-vision/intro-to-spatial-analysis-public-preview?tabs=sa), and [Face Overview](https://learn.microsoft.com/azure/ai-services/computer-vision/overview-identity). - **Support**: Support options for AI Services can be found here: [Azure AI services support and help options - Azure AI services | Microsoft Learn](https://learn.microsoft.com/azure/ai-services/cognitive-services-support-options?context=%2Fazure%2Fai-services%2Fcomputer-vision%2Fcontext%2Fcontext). ## Pricing View up-to-date pricing information for the pay-as-you-go pricing model here: [Azure AI Vision pricing](https://azure.microsoft.com/pricing/details/cognitive-services/computer-vision).).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddAzureAiVision(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Azure-AI-Vision", "1", "Microsoft");
    }

    /// <summary>
    /// Adds a deployment of Microsoft MAI-DS-R1 model (MAI-DS-R1 is a DeepSeek-R1 reasoning model that has been post-trained by the Microsoft AI team to fill in information gaps in the previous version of the model and improve its harm protections while maintaining R1 reasoning capabilities.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddMaiDsR1(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "MAI-DS-R1", "1", "Microsoft");
    }

    /// <summary>
    /// Adds a deployment of Microsoft model-router model (Model router is a deployable AI model that is trained to select the most suitable large language model (LLM) for a given prompt.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddModelRouter(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "model-router", "2025-08-07", "Microsoft");
    }

    /// <summary>
    /// Adds a deployment of Microsoft Phi-3-medium-128k-instruct model (Same Phi-3-medium model, but with a larger context size for RAG or few shot prompting.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddPhi3Medium128kInstruct(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Phi-3-medium-128k-instruct", "7", "Microsoft");
    }

    /// <summary>
    /// Adds a deployment of Microsoft Phi-3-medium-4k-instruct model (A 14B parameters model, proves better quality than Phi-3-mini, with a focus on high-quality, reasoning-dense data.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddPhi3Medium4kInstruct(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Phi-3-medium-4k-instruct", "6", "Microsoft");
    }

    /// <summary>
    /// Adds a deployment of Microsoft Phi-3-mini-128k-instruct model (Same Phi-3-mini model, but with a larger context size for RAG or few shot prompting.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddPhi3Mini128kInstruct(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Phi-3-mini-128k-instruct", "13", "Microsoft");
    }

    /// <summary>
    /// Adds a deployment of Microsoft Phi-3-mini-4k-instruct model (Tiniest member of the Phi-3 family. Optimized for both quality and low latency.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddPhi3Mini4kInstruct(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Phi-3-mini-4k-instruct", "15", "Microsoft");
    }

    /// <summary>
    /// Adds a deployment of Microsoft Phi-3-small-128k-instruct model (Same Phi-3-small model, but with a larger context size for RAG or few shot prompting.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddPhi3Small128kInstruct(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Phi-3-small-128k-instruct", "5", "Microsoft");
    }

    /// <summary>
    /// Adds a deployment of Microsoft Phi-3-small-8k-instruct model (A 7B parameters model, proves better quality than Phi-3-mini, with a focus on high-quality, reasoning-dense data.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddPhi3Small8kInstruct(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Phi-3-small-8k-instruct", "6", "Microsoft");
    }

    /// <summary>
    /// Adds a deployment of Microsoft Phi-3.5-mini-instruct model (Refresh of Phi-3-mini model.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddPhi35MiniInstruct(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Phi-3.5-mini-instruct", "6", "Microsoft");
    }

    /// <summary>
    /// Adds a deployment of Microsoft Phi-3.5-MoE-instruct model (A new mixture of experts model).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddPhi35MoeInstruct(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Phi-3.5-MoE-instruct", "5", "Microsoft");
    }

    /// <summary>
    /// Adds a deployment of Microsoft Phi-3.5-vision-instruct model (Refresh of Phi-3-vision model.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddPhi35VisionInstruct(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Phi-3.5-vision-instruct", "2", "Microsoft");
    }

    /// <summary>
    /// Adds a deployment of Microsoft Phi-4 model (Phi-4 14B, a highly capable model for low latency scenarios.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddPhi4(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Phi-4", "8", "Microsoft");
    }

    /// <summary>
    /// Adds a deployment of Microsoft Phi-4-mini-instruct model (3.8B parameters Small Language Model outperforming larger models in reasoning, math, coding, and function-calling).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddPhi4MiniInstruct(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Phi-4-mini-instruct", "1", "Microsoft");
    }

    /// <summary>
    /// Adds a deployment of Microsoft Phi-4-mini-reasoning model (Lightweight math reasoning model optimized for multi-step problem solving).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddPhi4MiniReasoning(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Phi-4-mini-reasoning", "1", "Microsoft");
    }

    /// <summary>
    /// Adds a deployment of Microsoft Phi-4-multimodal-instruct model (First small multimodal model to have 3 modality inputs (text, audio, image), excelling in quality and efficiency).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddPhi4MultimodalInstruct(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Phi-4-multimodal-instruct", "2", "Microsoft");
    }

    /// <summary>
    /// Adds a deployment of Microsoft Phi-4-reasoning model (State-of-the-art open-weight reasoning model.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddPhi4Reasoning(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Phi-4-reasoning", "1", "Microsoft");
    }

    // Mistral AI Models
    /// <summary>
    /// Adds a deployment of Mistral AI Codestral-2501 model (Codestral 25.01 by Mistral AI is designed for code generation, supporting 80+ programming languages, and optimized for tasks like code completion and fill-in-the-middle).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddCodestral2501(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Codestral-2501", "2", "Mistral AI");
    }

    /// <summary>
    /// Adds a deployment of Mistral AI Ministral-3B model (Ministral 3B is a state-of-the-art Small Language Model (SLM) optimized for edge computing and on-device applications. As it is designed for low-latency and compute-efficient inference, it it also the perfect model for standard GenAI applications that have).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddMinistral3B(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Ministral-3B", "1", "Mistral AI");
    }

    /// <summary>
    /// Adds a deployment of Mistral AI mistral-document-ai-2505 model (Document conversion to markdown with interleaved images and text).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddMistralDocumentAi2505(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "mistral-document-ai-2505", "1", "Mistral AI");
    }

    /// <summary>
    /// Adds a deployment of Mistral AI Mistral-large-2407 model (Mistral Large (2407) is an advanced Large Language Model (LLM) with state-of-the-art reasoning, knowledge and coding capabilities.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddMistralLarge2407(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Mistral-large-2407", "1", "Mistral AI");
    }

    /// <summary>
    /// Adds a deployment of Mistral AI Mistral-Large-2411 model (Mistral Large 24.11 offers enhanced system prompts, advanced reasoning and function calling capabilities.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddMistralLarge2411(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Mistral-Large-2411", "2", "Mistral AI");
    }

    /// <summary>
    /// Adds a deployment of Mistral AI mistral-medium-2505 model (Mistral Medium 3 is an advanced Large Language Model (LLM) with state-of-the-art reasoning, knowledge, coding and vision capabilities.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddMistralMedium2505(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "mistral-medium-2505", "1", "Mistral AI");
    }

    /// <summary>
    /// Adds a deployment of Mistral AI Mistral-Nemo model (Mistral Nemo is a cutting-edge Language Model (LLM) boasting state-of-the-art reasoning, world knowledge, and coding capabilities within its size category.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddMistralNemo(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Mistral-Nemo", "1", "Mistral AI");
    }

    /// <summary>
    /// Adds a deployment of Mistral AI Mistral-small model (Mistral Small can be used on any language-based task that requires high efficiency and low latency.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddMistralSmall(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "Mistral-small", "1", "Mistral AI");
    }

    /// <summary>
    /// Adds a deployment of Mistral AI mistral-small-2503 model (Enhanced Mistral Small 3 with multimodal capabilities and a 128k context length.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddMistralSmall2503(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "mistral-small-2503", "1", "Mistral AI");
    }

    // OpenAI Models
    /// <summary>
    /// Adds a deployment of OpenAI babbage-002 model (Babbage-002 is the latest versions of Babbage, GPT3 base models. Babbage-002 replaces the deprecated Ada and Babbage models. It is a smaller, faster model that is primarily used for fine tuning tasks. This model supports 16384 max input tokens and training data is up to Sep 2021. Bababge-002 supports fine-tuning, allowing developers and businesses to customize the model for specific applications. Your training data and validation data sets consist of input and output examples for how you would like the model to perform. The training and validation data you use must be formatted as a JSON Lines (JSONL) document in which each line represents a single prompt-completion pair. ## Model variation Babbage-002 is the latest version of Babbage, a gpt-3 based model. Learn more at &lt;https://learn.microsoft.com/azure/cognitive-services/openai/concepts/models&gt;).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddBabbage002(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "babbage-002", "2", "OpenAI");
    }

    /// <summary>
    /// Adds a deployment of OpenAI codex-mini model (codex-mini is a fine-tuned variant of the o4-mini model, designed to deliver rapid, instruction-following performance for developers working in CLI workflows. Whether you&apos;re automating shell commands, editing scripts, or refactoring repositories, Codex-Min).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddCodexMini(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "codex-mini", "2025-05-16", "OpenAI");
    }

    /// <summary>
    /// Adds a deployment of OpenAI dall-e-3 model (DALL-E 3 generates images from text prompts that are provided by the user. DALL-E 3 is generally available for use on Azure OpenAI. The image generation API creates an image from a text prompt. It does not edit existing images or create variations. Learn more at: &lt;https://learn.microsoft.com/azure/ai-services/openai/concepts/models#dall-e&gt;).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddDallE3(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "dall-e-3", "3.0", "OpenAI");
    }

    /// <summary>
    /// Adds a deployment of OpenAI davinci-002 model (Davinci-002 is the latest versions of Davinci, gpt-3 base models. Davinci-002 replaces the deprecated Curie and Davinci models. It is a smaller, faster model that is primarily used for fine tuning tasks. This model supports 16384 max input tokens and training data is up to Sep 2021. Davinci-002 supports fine-tuning, allowing developers and businesses to customize the model for specific applications. Your training data and validation data sets consist of input and output examples for how you would like the model to perform. The training and validation data you use must be formatted as a JSON Lines (JSONL) document in which each line represents a single prompt-completion pair. ## Model variation Davinci-002 is the latest version of Davinci, a gpt-3 based model. Learn more at &lt;https://learn.microsoft.com/azure/cognitive-services/openai/concepts/models&gt;).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddDavinci002(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "davinci-002", "3", "OpenAI");
    }

    /// <summary>
    /// Adds a deployment of OpenAI gpt-35-turbo model (The gpt-35-turbo (also known as ChatGPT) is the most capable and cost-effective model in the gpt-3.5 family which has been optimized for chat using the Chat Completions API. It is a language model designed for conversational interfaces and the model behaves differently than previous gpt-3 models. Previous models were text-in and text-out, meaning they accepted a prompt string and returned a completion to append to the prompt. However, the ChatGPT model is conversation-in and message-out. The model expects a prompt string formatted in a specific chat-like transcript format and returns a completion that represents a model-written message in the chat. Learn more at &lt;https://learn.microsoft.com/azure/cognitive-services/openai/concepts/models&gt;).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddGpt35Turbo(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "gpt-35-turbo", "0125", "OpenAI");
    }

    /// <summary>
    /// Adds a deployment of OpenAI gpt-35-turbo-16k model (gpt-3.5 models can understand and generate natural language or code. The most capable and cost effective model in the gpt-3.5 family is gpt-3.5-turbo, which has been optimized for chat and works well for traditional completions tasks as well. gpt-3.5-turbo is available for use with the Chat Completions API. gpt-3.5-turbo Instruct has similar capabilities to text-davinci-003 using the Completions API instead of the Chat Completions API. We recommend using gpt-3.5-turbo and gpt-3.5-turbo-instruct over &lt;a href=&quot;https://learn.microsoft.com/azure/ai-services/openai/concepts/legacy-models&quot; target=&quot;_blank&quot;&gt;legacy gpt-3.5 and gpt-3 models.&lt;/a&gt; - gpt-35-turbo - gpt-35-turbo-16k - gpt-35-turbo-instruct You can see the token context length supported by each model in the model summary table. To learn more about how to interact with gpt-3.5-turbo and the Chat Completions API check out our &lt;a href=&quot;https://learn.microsoft.com/azure/ai-services/openai/how-to/chatgpt?tabs=python&amp;pivots=programming-language-chat-completions&quot; target=&quot;_blank&quot;&gt;in-depth how-to.&lt;/a&gt; | Model ID            | Model Availability                                                             | Max Request (tokens)    | Training Data (up to) | | ------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------ | --------------------------- | --------------------- | | gpt-35-turbo&lt;sup&gt;1&lt;/sup&gt; (0301) | East US, France Central, South Central US, UK South, West Europe                                      | 4,096            | Sep 2021       | | gpt-35-turbo (0613)       | Australia East, Canada East, East US, East US 2, France Central, Japan East, North Central US, Sweden Central, Switzerland North, UK South | 4,096            | Sep 2021       | | gpt-35-turbo-16k (0613)     | Australia East, Canada East, East US, East US 2, France Central, Japan East, North Central US, Sweden Central, Switzerland North, UK South | 16,384           | Sep 2021       | | gpt-35-turbo-instruct (0914)  | East US, Sweden Central                                                          | 4,097            | Sep 2021       | | gpt-35-turbo (1106)       | Australia East, Canada East, France Central, South India, Sweden Central, UK South, West US                        | Input: 16,385 Output: 4,096 | Sep 2021       | &lt;sup&gt;1&lt;/sup&gt; This model will accept requests &gt; 4,096 tokens. It is not recommended to exceed the 4,096 input token limit as the newer version of the model are capped at 4,096 tokens. If you encounter issues when exceeding 4,096 input tokens with this model this configuration is not officially supported.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddGpt35Turbo16k(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "gpt-35-turbo-16k", "0613", "OpenAI");
    }

    /// <summary>
    /// Adds a deployment of OpenAI gpt-35-turbo-instruct model (gpt-3.5 models can understand and generate natural language or code. The most capable and cost effective model in the gpt-3.5 family is gpt-3.5-turbo, which has been optimized for chat and works well for traditional completions tasks as well. gpt-3.5-turbo is available for use with the Chat Completions API. gpt-3.5-turbo-instruct has similar capabilities to text-davinci-003 using the Completions API instead of the Chat Completions API. We recommend using gpt-3.5-turbo and gpt-3.5-turbo-instruct over &lt;a href=&quot;https://learn.microsoft.com/azure/ai-services/openai/concepts/legacy-models&quot; target=&quot;_blank&quot;&gt;legacy gpt-3.5 and gpt-3 models.&lt;/a&gt; - gpt-35-turbo - gpt-35-turbo-16k - gpt-35-turbo-instruct You can see the token context length supported by each model in the model summary table. To learn more about how to interact with GPT-3.5 Turbo and the Chat Completions API check out our &lt;a href=&quot;https://learn.microsoft.com/azure/ai-services/openai/how-to/chatgpt?tabs=python&amp;pivots=programming-language-chat-completions&quot; target=&quot;_blank&quot;&gt;in-depth how-to.&lt;/a&gt; | Model ID            | Model Availability                                                             | Max Request (tokens)    | Training Data (up to) | | ------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------ | --------------------------- | --------------------- | | gpt-35-turbo&lt;sup&gt;1&lt;/sup&gt; (0301) | East US, France Central, South Central US, UK South, West Europe                                      | 4,096            | Sep 2021       | | gpt-35-turbo (0613)       | Australia East, Canada East, East US, East US 2, France Central, Japan East, North Central US, Sweden Central, Switzerland North, UK South | 4,096            | Sep 2021       | | gpt-35-turbo-16k (0613)     | Australia East, Canada East, East US, East US 2, France Central, Japan East, North Central US, Sweden Central, Switzerland North, UK South | 16,384           | Sep 2021       | | gpt-35-turbo-instruct (0914)  | East US, Sweden Central                                                          | 4,097            | Sep 2021       | | gpt-35-turbo (1106)       | Australia East, Canada East, France Central, South India, Sweden Central, UK South, West US                        | Input: 16,385 Output: 4,096 | Sep 2021       | &lt;sup&gt;1&lt;/sup&gt; This model will accept requests &gt; 4,096 tokens. It is not recommended to exceed the 4,096 input token limit as the newer version of the model are capped at 4,096 tokens. If you encounter issues when exceeding 4,096 input tokens with this model this configuration is not officially supported.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddGpt35TurboInstruct(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "gpt-35-turbo-instruct", "0914", "OpenAI");
    }

    /// <summary>
    /// Adds a deployment of OpenAI gpt-4 model (gpt-4 is a large multimodal model that accepts text or image inputs and outputs text. It can solve complex problems with greater accuracy than any of our previous models, thanks to its extensive general knowledge and advanced reasoning capabilities. gpt-4 provides a wide range of model versions to fit your business needs. Please note that AzureML Studio only supports the deployment of the gpt-4-0314 model version and AI Studio supports the deployment of all the model versions listed below. - **gpt-4-turbo-2024-04-09:** This is the GPT-4 Turbo with Vision GA model. The context window is 128,000 tokens, and it can return up to 4,096 output tokens. The training data is current up to December 2023. - **gpt-4-1106-preview (GPT-4 Turbo):** The latest gpt-4 model with improved instruction following, JSON mode, reproducible outputs, parallel function calling, and more. It returns a maximum of 4,096 output tokens. This preview model is not yet suited for production traffic. Context window: 128,000 tokens. Training Data: Up to April 2023. - **gpt-4-vision Preview (GPT-4 Turbo with vision):** This multimodal AI model enables users to direct the model to analyze image inputs they provide, along with all the other capabilities of GPT-4 Turbo. It can return up to 4,096 output tokens. As a preview model version, it is not yet suitable for production traffic. The context window is 128,000 tokens. Training data is current up to April 2023. - **gpt-4-0613:** gpt-4 model with a context window of 8,192 tokens. Training data up to September 2021. - **gpt-4-0314:** gpt-4 legacy model with a context window of 8,192 tokens. Training data up to September 2021. This model version will be retired no earlier than July 5, 2024. Learn more at &lt;https://learn.microsoft.com/azure/cognitive-services/openai/concepts/models&gt;).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddGpt4(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "gpt-4", "turbo-2024-04-09", "OpenAI");
    }

    /// <summary>
    /// Adds a deployment of OpenAI gpt-4-32k model (gpt-4 can solve difficult problems with greater accuracy than any of the previous OpenAI models. Like gpt-35-turbo, gpt-4 is optimized for chat but works well for traditional completions tasks. The gpt-4 supports 8192 max input tokens and the gpt-4-32k supports up to 32,768 tokens. Note: this model can be deployed for inference, but cannot be finetuned. Learn more at &lt;https://learn.microsoft.com/azure/cognitive-services/openai/concepts/models&gt;).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddGpt432k(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "gpt-4-32k", "0613", "OpenAI");
    }

    /// <summary>
    /// Adds a deployment of OpenAI gpt-4.1 model (gpt-4.1 outperforms gpt-4o across the board, with major gains in coding, instruction following, and long-context understanding).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddGpt41(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "gpt-4.1", "2025-04-14", "OpenAI");
    }

    /// <summary>
    /// Adds a deployment of OpenAI gpt-4.1-mini model (gpt-4.1-mini outperform gpt-4o-mini across the board, with major gains in coding, instruction following, and long-context handling).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddGpt41Mini(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "gpt-4.1-mini", "2025-04-14", "OpenAI");
    }

    /// <summary>
    /// Adds a deployment of OpenAI gpt-4.1-nano model (gpt-4.1-nano provides gains in coding, instruction following, and long-context handling along with lower latency and cost).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddGpt41Nano(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "gpt-4.1-nano", "2025-04-14", "OpenAI");
    }

    /// <summary>
    /// Adds a deployment of OpenAI gpt-4o model (OpenAI&apos;s most advanced multimodal model in the gpt-4o family. Can handle both text and image inputs.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddGpt4o(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "gpt-4o", "2024-11-20", "OpenAI");
    }

    /// <summary>
    /// Adds a deployment of OpenAI gpt-4o-audio-preview model (Best suited for rich, asynchronous audio input/output interactions, such as creating spoken summaries from text.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddGpt4oAudioPreview(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "gpt-4o-audio-preview", "2024-12-17", "OpenAI");
    }

    /// <summary>
    /// Adds a deployment of OpenAI gpt-4o-mini model (An affordable, efficient AI solution for diverse text and image tasks.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddGpt4oMini(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "gpt-4o-mini", "2024-07-18", "OpenAI");
    }

    /// <summary>
    /// Adds a deployment of OpenAI gpt-4o-mini-audio-preview model (Best suited for rich, asynchronous audio input/output interactions, such as creating spoken summaries from text.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddGpt4oMiniAudioPreview(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "gpt-4o-mini-audio-preview", "2024-12-17", "OpenAI");
    }

    /// <summary>
    /// Adds a deployment of OpenAI gpt-4o-mini-realtime-preview model (Best suited for rich, asynchronous audio input/output interactions, such as creating spoken summaries from text.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddGpt4oMiniRealtimePreview(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "gpt-4o-mini-realtime-preview", "2024-12-17", "OpenAI");
    }

    /// <summary>
    /// Adds a deployment of OpenAI gpt-4o-mini-transcribe model (A highly efficient and cost effective speech-to-text solution that deliverables reliable and accurate transcripts.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddGpt4oMiniTranscribe(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "gpt-4o-mini-transcribe", "2025-03-20", "OpenAI");
    }

    /// <summary>
    /// Adds a deployment of OpenAI gpt-4o-mini-tts model (An advanced text-to-speech solution designed to convert written text into natural-sounding speech.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddGpt4oMiniTts(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "gpt-4o-mini-tts", "2025-03-20", "OpenAI");
    }

    /// <summary>
    /// Adds a deployment of OpenAI gpt-4o-realtime-preview model (The gpt-4o-realtime-preview model introduces a new era in AI interaction by incorporating the new audio modality powered by gpt-4o. This new modality allows for seamless speech-to-speech and text-to-speech applications, providing a richer and more engaging user experience. Engineered for speed and efficiency, gpt-4o-realtime-preview handles complex audio queries with minimal resources, translating into improved audio performance. The introduction of gpt-4o-realtime-preview opens numerous possibilities for businesses in various sectors: - **Enhanced customer service:** By integrating audio inputs, gpt-4o-realtime-preview enables more dynamic and comprehensive customer support interactions. - **Content innovation:** Use gpt-4o-realtime-preview&apos;s generative capabilities to create engaging and diverse audio content, catering to a broad range of consumer preferences. - **Real-time translation:** Leverage gpt-4o-realtime-preview&apos;s capability to provide accurate and immediate translations, facilitating seamless communication across different languages Model Versions: - **2024-12-17:** Updating the gpt-4o-realtime-preview model with improvements in voice quality and input reliability. As this is a preview version, it is designed for testing and feedback purposes and is not yet optimized for production traffic. - **2024-10-01:** Introducing our new multimodal AI model, which now supports both text and audio modalities. As this is a preview version, it is designed for testing and feedback purposes and is not yet optimized for production traffic.  ## Limitations _IMPORTANT: The system stores your prompts and completions as described in the &quot;Data Use and Access for Abuse Monitoring&quot; section of the service-specific Product Terms for Azure OpenAI Service, except that the Limited Exception does not apply. Abuse monitoring will be turned on for use of the GPT-4o-realtime-preview API even for customers who otherwise are approved for modified abuse monitoring._ Currently, the gpt-4o-realtime-preview model focuses on text and audio and does not support existing gpt-4o features such as image modality and structured outputs. For many tasks, the generally available gpt-4o models may still be more suitable. _IMPORTANT: At this time, gpt-4o-realtime-preview usage limits are suitable for test and development. To prevent abuse and preserve service integrity, rate limits will be adjusted as needed._).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddGpt4oRealtimePreview(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "gpt-4o-realtime-preview", "2024-12-17", "OpenAI");
    }

    /// <summary>
    /// Adds a deployment of OpenAI gpt-4o-transcribe model (A cutting-edge speech-to-text solution that deliverables reliable and accurate transcripts.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddGpt4oTranscribe(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "gpt-4o-transcribe", "2025-03-20", "OpenAI");
    }

    /// <summary>
    /// Adds a deployment of OpenAI gpt-5-chat model (gpt-5-chat (preview) is an advanced, natural, multimodal, and context-aware conversations for enterprise applications.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddGpt5Chat(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "gpt-5-chat", "2025-08-07", "OpenAI");
    }

    /// <summary>
    /// Adds a deployment of OpenAI gpt-5-mini model (gpt-5-mini is a lightweight version for cost-sensitive applications.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddGpt5Mini(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "gpt-5-mini", "2025-08-07", "OpenAI");
    }

    /// <summary>
    /// Adds a deployment of OpenAI gpt-5-nano model (gpt-5-nano is optimized for speed, ideal for applications requiring low latency.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddGpt5Nano(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "gpt-5-nano", "2025-08-07", "OpenAI");
    }

    /// <summary>
    /// Adds a deployment of OpenAI gpt-oss-120b model (Push the open model frontier with GPT-OSS models, released under the permissive Apache 2.0 license, allowing anyone to use, modify, and deploy them freely.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddGptOss120b(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "gpt-oss-120b", "3", "OpenAI");
    }

    /// <summary>
    /// Adds a deployment of OpenAI o1 model (Focused on advanced reasoning and solving complex problems, including math and science tasks. Ideal for applications that require deep contextual understanding and agentic workflows.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddO1(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "o1", "2024-12-17", "OpenAI");
    }

    /// <summary>
    /// Adds a deployment of OpenAI o1-mini model (Smaller, faster, and 80% cheaper than o1-preview, performs well at code generation and small context operations.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddO1Mini(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "o1-mini", "2024-09-12", "OpenAI");
    }

    /// <summary>
    /// Adds a deployment of OpenAI o3-mini model (o3-mini includes the o1 features with significant cost-efficiencies for scenarios requiring high performance.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddO3Mini(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "o3-mini", "2025-01-31", "OpenAI");
    }

    /// <summary>
    /// Adds a deployment of OpenAI o4-mini model (o4-mini includes significant improvements on quality and safety while supporting the existing features of o3-mini and delivering comparable or better performance.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddO4Mini(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "o4-mini", "2025-04-16", "OpenAI");
    }

    /// <summary>
    /// Adds a deployment of OpenAI sora model (An efficient AI solution to generate videos).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddSora(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "sora", "2025-05-02", "OpenAI");
    }

    /// <summary>
    /// Adds a deployment of OpenAI text-embedding-3-large model (Text-embedding-3 series models are the latest and most capable embedding model from OpenAI.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddTextEmbedding3Large(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "text-embedding-3-large", "1", "OpenAI");
    }

    /// <summary>
    /// Adds a deployment of OpenAI text-embedding-3-small model (Text-embedding-3 series models are the latest and most capable embedding model from OpenAI.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddTextEmbedding3Small(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "text-embedding-3-small", "1", "OpenAI");
    }

    /// <summary>
    /// Adds a deployment of OpenAI text-embedding-ada-002 model (text-embedding-ada-002 outperforms all the earlier embedding models on text search, code search, and sentence similarity tasks and gets comparable performance on text classification. Embeddings are numerical representations of concepts converted to number sequences, which make it easy for computers to understand the relationships between those concepts. Note: this model can be deployed for inference, specifically for embeddings, but cannot be finetuned. ## Model variation text-embedding-ada-002 is part of gpt-3 model family. Learn more at &lt;https://learn.microsoft.com/azure/cognitive-services/openai/concepts/models#embeddings-models&gt;).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddTextEmbeddingAda002(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "text-embedding-ada-002", "2", "OpenAI");
    }

    /// <summary>
    /// Adds a deployment of OpenAI tts model (TTS is a model that converts text to natural sounding speech. TTS is optimized for realtime or interactive scenarios. For offline scenarios, TTS-HD provides higher quality. The API supports six different voices. Max request data size: 4,096 chars can be converted from text to speech per API request. ## Model Variants - TTS: optimized for speed. - TTS-HD: optimized for quality.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddTts(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "tts", "001", "OpenAI");
    }

    /// <summary>
    /// Adds a deployment of OpenAI tts-hd model (TTS-HD is a model that converts text to natural sounding speech. TTS is optimized for realtime or interactive scenarios. For offline scenarios, TTS-HD provides higher quality. The API supports six different voices. Max request data size: 4,096 chars can be converted from text to speech per API request. ## Model Variants - TTS: optimized for speed. - TTS-HD: optimized for quality.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddTtsHd(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "tts-hd", "001", "OpenAI");
    }

    /// <summary>
    /// Adds a deployment of OpenAI whisper model (The Whisper models are trained for speech recognition and translation tasks, capable of transcribing speech audio into the text in the language it is spoken (automatic speech recognition) as well as translated into English (speech translation). Researchers at OpenAI developed the models to study the robustness of speech processing systems trained under large-scale weak supervision. The model version 001 corresponds to whisper large v2. Max request data size: 25mb of audio can be converted from speech to text per API request.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddWhisper(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "whisper", "001", "OpenAI");
    }

    // xAI Models
    /// <summary>
    /// Adds a deployment of xAI grok-3 model (Grok 3 is xAI&apos;s debut model, pretrained by Colossus at supermassive scale to excel in specialized domains like finance, healthcare, and the law.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddGrok3(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "grok-3", "1", "xAI");
    }

    /// <summary>
    /// Adds a deployment of xAI grok-3-mini model (Grok 3 Mini is a lightweight model that thinks before responding. Trained on mathematic and scientific problems, it is great for logic-based tasks.).
    /// </summary>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddGrok3Mini(
        this IResourceBuilder<AzureAIFoundryResource> foundry,
        string deploymentName)
    {
        return foundry.AddDeployment(deploymentName, "grok-3-mini", "1", "xAI");
    }

}
