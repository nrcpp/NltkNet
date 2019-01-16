using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Collections;
using NltkNet;
using static NltkNet.BuiltIns;

namespace TestApp
{
    static class TestClassify
    {
        public static void TestNames()
        {            
            //def gender_features(word):
            //    return { 'last_letter': word[-1]}
            dynamic GenderFeature(string word)
            {
                var dict = BuiltIns.Dict();
                dict["last_letter"] = string.IsNullOrEmpty(word) ? "" : word.Last().ToString();
                return dict;
            }

            //from nltk.corpus import names
            //>>> labeled_names = ([(name, 'male') for name in names.words('male.txt')] +
            //... [(name, 'female') for name in names.words('female.txt')])
            var namesCorpus = new NltkNet.Nltk.Corpus.Names();
            var maleNames = namesCorpus.Words("male.txt").AsNet.Select(name => (name, gender:"male"));
            var femaleNames = namesCorpus.Words("female.txt").AsNet.Select(name => (name, gender:"female"));
            var labeledNames = maleNames.ToList();
            labeledNames.AddRange(femaleNames);

            //>>> import random
            //>>> random.shuffle(labeled_names)
            labeledNames.Shuffle();
            
            //featuresets = [(gender_features(n), gender) for (n, gender) in labeled_names]
            //train_set, test_set = featuresets[500:], featuresets[:500]
            var featuresets = labeledNames.Select(nameAndGender => (GenderFeature(nameAndGender.name), nameAndGender.gender)).ToList();
            var train_set = featuresets.Take(500).ToList();
            var test_set = featuresets.TakeLast(500).ToList();

            // nltk.NaiveBayesClassifier.train(train_set)
            dynamic classifier = Nltk.NaiveBayesClassifier.Train(train_set);

            // classifier.classify(gender_features('Neo'))
            Print(classifier.classify(GenderFeature("Neo")));
        }
    }
}
