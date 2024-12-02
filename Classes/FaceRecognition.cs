using Tensorflow;
using System;
using System.Drawing; // For handling Bitmap

public class FaceRecognition
{
    private static Graph graph;
    private static Session session;

    // Load model function to load a .pb file
    public static void LoadModel(string modelPath)
    {
        // Initialize the graph and session
        graph = new Graph();
        session = new Session(graph);

        // Import the model into the graph
        graph.Import(modelPath);
    }

    // Load multiple models (if needed)
    public static void LoadModels()
    {
        // Load your first FaceNet model
        string modelPath1 = @"path_to_your_model\20180408-102900.pb";
        LoadModel(modelPath1);
        Console.WriteLine("Model 1 loaded successfully.");

        // Load your second FaceNet model (if you have another model)
        string modelPath2 = @"path_to_your_second_model\model2.pb";
        LoadModel(modelPath2);
        Console.WriteLine("Model 2 loaded successfully.");
    }

    // Get embeddings from a specific model
    // Get embeddings from a specific model
    // Get embeddings from a specific model
    //public static float[] GetEmbeddingsAlternative(Bitmap preprocessedFace)
    //{
    //    // Convert the image to a Tensor
    //    Tensor inputTensor = TensorHelper.ConvertImageToTensor(preprocessedFace);

    //    // Retrieve the operation names for the input and embeddings
    //    var inputOp = graph.OperationByName("input:0");
    //    var phaseTrainOp = graph.OperationByName("phase_train:0");
    //    var embeddingsOp = graph.OperationByName("embeddings:0");

    //    // Create a list of feed items to pass inputs to the session
    //    var feedItems = new List<FeedItem>
    //{
    //    new FeedItem(inputOp, inputTensor),
    //    new FeedItem(phaseTrainOp, new Tensor(false))  // Set phase_train to false for inference
    //};

    //    // Instead of using the "run" method, use session.Run() with a tuple approach
    //    var results = session.Run(
    //        feedItems.ToArray(),  // Convert feedItems to an array
    //        new Operation[] { embeddingsOp } // Specify the operations (embeddings)
    //    );

    //    // Extract the embedding result from the session's output
    //    float[] embeddings = results[0].ToArray<float>();
    //    return embeddings;
    //}


}
