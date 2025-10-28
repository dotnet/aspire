// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Aspire.Hosting.Azure;

/// <summary>
/// Generated strongly typed model descriptors for Azure AI Foundry.
/// </summary>
public partial class AIFoundryModel
{
    /// <summary>
    /// Models available on Foundry Local.
    /// </summary>
    public static class Local
    {
        /// <summary>
        /// This model is an optimized version of DeepSeek-R1-Distill-Qwen-14B to enable local inference. This model uses RTN quantization. # Model Description - **Developed by:** Microsoft - **Model type:** ONNX - **License:** MIT - **Model Description:** This is a conversion of the DeepSeek-R1-Distill-Qwen-14B for local inference. - **Disclaimer:** Model is only an optimization of the base model, any risk associated with the model is the responsibility of the user of the model. Please verify and test for your scenarios. There may be a slight difference in output from the base model with the optimizations applied. Note that optimizations applied are distinct from fine tuning and thus do not alter the intended uses or capabilities of the model. # Base Model Information See Hugging Face model [DeepSeek-R1-Distill-Qwen-14B](https://huggingface.co/deepseek-ai/DeepSeek-R1-Distill-Qwen-14B) for details.
        /// </summary>
        public static readonly AIFoundryModel DeepseekR114b = new() { Name = "deepseek-r1-14b", Version = "3", Format = "Microsoft" };

        /// <summary>
        /// This model is an optimized version of DeepSeek-R1-Distill-Qwen-7B to enable local inference. This model uses RTN quantization. # Model Description - **Developed by:** Microsoft - **Model type:** ONNX - **License:** MIT - **Model Description:** This is a conversion of the DeepSeek-R1-Distill-Qwen-7B for local inference. - **Disclaimer:** Model is only an optimization of the base model, any risk associated with the model is the responsibility of the user of the model. Please verify and test for your scenarios. There may be a slight difference in output from the base model with the optimizations applied. Note that optimizations applied are distinct from fine tuning and thus do not alter the intended uses or capabilities of the model. # Base Model Information See Hugging Face model [DeepSeek-R1-Distill-Qwen-7B](https://huggingface.co/deepseek-ai/DeepSeek-R1-Distill-Qwen-7B) for details.
        /// </summary>
        public static readonly AIFoundryModel DeepseekR17b = new() { Name = "deepseek-r1-7b", Version = "3", Format = "Microsoft" };

        /// <summary>
        /// This model is an optimized version of gpt-oss-20b to enable local inference. This model uses RTN quantization. # Model Description - **Developed by:** Microsoft - **Model type:** ONNX - **License:** Apache-2.0 - **License Description:** Use of this model is subject to the terms of the Apache License, Version 2.0, available at http://www.apache.org/licenses/LICENSE-2.0. - **Model Description:** This is a conversion of the gpt-oss-20b model for local inference. - **Disclaimer:** Model is only an optimization of the base model, any risk associated with the model is the responsibility of the user of the model. Please verify and test for your scenarios. There may be a slight difference in output from the base model with the optimizations applied. Note that optimizations applied are distinct from fine tuning and thus do not alter the intended uses or capabilities of the model. # Base Model Information See Azure AI Foundry model [gpt-oss-20b](https://ai.azure.com/catalog/models/gpt-oss-20b) for details.
        /// </summary>
        public static readonly AIFoundryModel GptOss20b = new() { Name = "gpt-oss-20b", Version = "1", Format = "Microsoft" };

        /// <summary>
        /// This model is an optimized version of Mistral-7B-Instruct-v0.2 to enable local inference. This model uses RTN quantization. # Model Description - **Developed by:** Microsoft - **Model type:** ONNX - **License:** apache-2.0 - **Model Description:** This is a conversion of the Mistral-7B-Instruct-v0.2 for local inference. - **Disclaimer:** Model is only an optimization of the base model, any risk associated with the model is the responsibility of the user of the model. Please verify and test for your scenarios. There may be a slight difference in output from the base model with the optimizations applied. Note that optimizations applied are distinct from fine tuning and thus do not alter the intended uses or capabilities of the model. # Base Model Information See Hugging Face model [Mistral-7B-Instruct-v0.2](https://huggingface.co/mistralai/Mistral-7B-Instruct-v0.2) for details.
        /// </summary>
        public static readonly AIFoundryModel Mistral7bV02 = new() { Name = "mistral-7b-v0.2", Version = "1", Format = "Microsoft" };

        /// <summary>
        /// This model is an optimized version of Phi-3-Mini-128K-Instruct to enable local inference. This model uses RTN quantization. # Model Description - **Developed by:** Microsoft - **Model type:** ONNX - **License:** MIT - **Model Description:** This is a conversion of the Phi-3-Mini-128K-Instruct for local inference. - **Disclaimer:** Model is only an optimization of the base model, any risk associated with the model is the responsibility of the user of the model. Please verify and test for your scenarios. There may be a slight difference in output from the base model with the optimizations applied. Note that optimizations applied are distinct from fine tuning and thus do not alter the intended uses or capabilities of the model. # Base Model Information See Hugging Face model [Phi-3-Mini-128K-Instruct](https://huggingface.co/microsoft/Phi-3-Mini-128K-Instruct) for details.
        /// </summary>
        public static readonly AIFoundryModel Phi3Mini128k = new() { Name = "phi-3-mini-128k", Version = "1", Format = "Microsoft" };

        /// <summary>
        /// This model is an optimized version of Phi-3-Mini-4K-Instruct to enable local inference. This model uses RTN quantization. # Model Description - **Developed by:** Microsoft - **Model type:** ONNX - **License:** MIT - **Model Description:** This is a conversion of the Phi-3-Mini-4K-Instruct for local inference. - **Disclaimer:** Model is only an optimization of the base model, any risk associated with the model is the responsibility of the user of the model. Please verify and test for your scenarios. There may be a slight difference in output from the base model with the optimizations applied. Note that optimizations applied are distinct from fine tuning and thus do not alter the intended uses or capabilities of the model. # Base Model Information See Hugging Face model [Phi-3-Mini-4K-Instruct](https://huggingface.co/microsoft/Phi-3-mini-4k-instruct) for details.
        /// </summary>
        public static readonly AIFoundryModel Phi3Mini4k = new() { Name = "phi-3-mini-4k", Version = "1", Format = "Microsoft" };

        /// <summary>
        /// This model is an optimized version of Phi-3.5-mini-instruct to enable local inference. This model uses RTN quantization. # Model Description - **Developed by:** Microsoft - **Model type:** ONNX - **License:** MIT - **Model Description:** This is a conversion of the Phi-3.5-mini-instruct for local inference. - **Disclaimer:** Model is only an optimization of the base model, any risk associated with the model is the responsibility of the user of the model. Please verify and test for your scenarios. There may be a slight difference in output from the base model with the optimizations applied. Note that optimizations applied are distinct from fine tuning and thus do not alter the intended uses or capabilities of the model. # Base Model Information See Hugging Face model [Phi-3.5-mini-instruct](https://huggingface.co/microsoft/Phi-3.5-mini-instruct) for details.
        /// </summary>
        public static readonly AIFoundryModel Phi35Mini = new() { Name = "phi-3.5-mini", Version = "1", Format = "Microsoft" };

        /// <summary>
        /// This model is an optimized version of Phi-4 to enable local inference. This model uses RTN quantization. # Model Description - **Developed by:** Microsoft - **Model type:** ONNX - **License:** MIT - **Model Description:** This is a conversion of the Phi-4 for local inference. - **Disclaimer:** Model is only an optimization of the base model, any risk associated with the model is the responsibility of the user of the model. Please verify and test for your scenarios. There may be a slight difference in output from the base model with the optimizations applied. Note that optimizations applied are distinct from fine tuning and thus do not alter the intended uses or capabilities of the model. # Base Model Information See Hugging Face model [Phi-4](https://huggingface.co/microsoft/Phi-4) for details.
        /// </summary>
        public static readonly AIFoundryModel Phi4 = new() { Name = "phi-4", Version = "1", Format = "Microsoft" };
    }
}
