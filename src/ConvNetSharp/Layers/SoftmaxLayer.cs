using System;
using System.Runtime.Serialization;

namespace ConvNetSharp.Layers
{
    /// <summary>
    ///     This is a classifier, with N discrete classes from 0 to N-1
    ///     it gets a stream of N incoming numbers and computes the softmax
    ///     function (exponentiate and normalize to sum to 1 as probabilities should)
    /// </summary>
    [DataContract]
    [Serializable]
    public class SoftmaxLayer : LastLayerBase, IClassificationLayer
    {
        [DataMember]
        private double[] es;

        public SoftmaxLayer(int classCount)
        {
            this.ClassCount = classCount;
        }

        [DataMember]
        public int ClassCount { get; set; }

        public override double Backward(double y)
        {
            var yint = (int)y;

            // compute and accumulate gradient wrt weights and bias of this layer
            var x = this.InputActivation;
            x.ZeroGradients(); // zero out the gradient of input Vol

            for (var i = 0; i < this.OutputDepth; i++)
            {
                var indicator = i == yint ? 1.0 : 0.0;
                var mul = -(indicator - this.es[i]);
                x.SetWeightGradient(i, mul);
            }

            // loss is the class negative log likelihood
            return -Math.Log(this.es[yint]);
        }

        public override double Backward(double[] y)
        {
            throw new NotImplementedException();
        }

        public override IVolume Forward(IVolume input, bool isTraining = false)
        {
            this.InputActivation = input;

            var outputActivation = new Volume(1, 1, this.OutputDepth, 0.0);

            // compute max activation
            var amax = input.GetWeight(0);
            for (var i = 1; i < this.OutputDepth; i++)
            {
                if (input.GetWeight(i) > amax)
                {
                    amax = input.GetWeight(i);
                }
            }

            // compute exponentials (carefully to not blow up)
            var es = new double[this.OutputDepth];
            var esum = 0.0;
            for (var i = 0; i < this.OutputDepth; i++)
            {
                var e = Math.Exp(input.GetWeight(i) - amax);
                esum += e;
                es[i] = e;
            }

            // normalize and output to sum to one
            for (var i = 0; i < this.OutputDepth; i++)
            {
                es[i] /= esum;
                outputActivation.SetWeight(i, es[i]);
            }

            this.es = es; // save these for backprop
            this.OutputActivation = outputActivation;
            return this.OutputActivation;
        }

        public override void Backward()
        {
            throw new NotImplementedException();
        }

        public override void Init(int inputWidth, int inputHeight, int inputDepth)
        {
            base.Init(inputWidth, inputHeight, inputDepth);

            var inputCount = inputWidth * inputHeight * inputDepth;
            this.OutputDepth = inputCount;
            this.OutputWidth = 1;
            this.OutputHeight = 1;
        }
    }
}