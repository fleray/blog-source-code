﻿using System;
using System.Collections.Generic;
using System.Configuration;
using Couchbase.Configuration.Client;
using Couchbase.Core;
using Couchbase.IO;

namespace Couchbase.Examples.SubDocumentAPI
{
    class Program
    {
        static void Main(string[] args)
        {
            ClusterHelper.Initialize(new ClientConfiguration
            {
                Servers = new List<Uri>
                {
                    new Uri(ConfigurationManager.AppSettings["couchbaseUri"])
                }
            });

            var bucket = ClusterHelper.GetBucket("default");
            const string id = "puppy";
            var dog = new
            {
                Type = "dog",
                Breed = "Pitbull/Chihuahua",
                Name = "Puppy",
                Toys = new List<string> {"squeaker", "ball", "shoe"},
                Owner = new
                {
                    Type = "servant",
                    Name = "Don Knotts",
                    Age = 63
                },
                Attributes = new Dictionary<string, object>
                {
                    {"Fleas", true},
                    {"Color", "white"},
                    {"EyeColor", "brown"},
                    {"Age", 5},
                    {"Dirty", true },
                    {"Sex", "female" }
                },
                Counts = new List<object> { 1}
            };

            Reset(bucket, id, dog);
            LookupInChaining(bucket, id);

            Reset(bucket, id, dog);
            // tag::RemoveExampleCall[]
            RemoveExample(bucket, id, "owner.name");
            // end::RemoveExampleCall[]

            Reset(bucket, id, dog);
            ErrorExample(bucket, id);

            Reset(bucket, id, dog);
            GetExample(bucket, "owner", id);

            Reset(bucket, id, dog);
            // tag::InsertExampleCall[]
            InsertExample(bucket, id,"attributes.hairLength", "short");
            // end::InsertExampleCall[]

            Reset(bucket, id, dog);
            // tag::InsertExampleCallWithNewAttribute[]
            InsertExample(bucket, id, "anewattribute.withakey", "somevalue");
            // end::InsertExampleCallWithNewAttribute[]

            Reset(bucket, id, dog);
            // tag::ReplaceExampleCall[]
            ReplaceExample(bucket, id, "owner", new { CatLover=true, CatName="celia"});
            // end::ReplaceExampleCall[]

            Reset(bucket, id, dog);
            // tag::ArrayAppendExampleCall[]
            ArrayAppendExample(bucket, id, "toys", "slipper");
            // end::ArrayAppendExampleCall[]

            Reset(bucket, id, dog);
            // tag::ArrayPrependExampleCall[]
            ArrayPrependExample(bucket, id, "toys", "slipper");
            // end::ArrayPrependExampleCall[]

            Reset(bucket, id, dog);
            // tag::ArrayInsertExampleCall[]
            ArrayInsertExample(bucket, id, "toys[2]", "slipper");
            // end::ArrayInsertExampleCall[]

            Reset(bucket, id, dog);
            // tag::ArrayAddUniqueExampleCall[]
            ArrayAddUniqueExample(bucket, id, "toys", "shoe");
            // end::ArrayAddUniqueExampleCall[]

            Reset(bucket, id, dog);
            // tag::CounterExampleCalls[]
            CounterExample(bucket, id, "likes", 1);
            CounterExample(bucket, id, "likes", -1);
            // end::CounterExampleCalls[]

            ClusterHelper.Close();
            Console.Read();
        }

        static void Reset(IBucket bucket, string id, dynamic doc)
        {
            var result = bucket.Upsert(new Document<dynamic>
            {
                Id = id,
                Content = doc
            });
            Console.WriteLine("Updated {0} - {1}", id, result.Status);
        }

        public static void LookupInChaining(IBucket bucket, string id)
        {
            // tag::LookupInExample[]
            var builder = bucket.LookupIn<dynamic>(id).
                Get("type").
                Get("name").
                Get("owner").
                Exists("notfound");
            // end::LookupInExample[]

            // tag::LookupInExampleExecute[]
            var fragment = builder.Execute();
            // end::LookupInExampleExecute[]

            // tag::LookupInExampleResult[]
            if (fragment.OpStatus("type") == ResponseStatus.Success)
            {
                string format = "Path='{0}' Value='{1}'";
                Console.WriteLine(format, "type", fragment.Content("type"));
            }
            // end::LookupInExampleResult[]
            GetDisplay(fragment, "type");
            GetDisplay(fragment, "name");
            GetDisplay(fragment, "owner");
        }

        static void GetDisplay<T>(IDocumentFragment<T> fragment, string path)
        {
            string format = "Path='{0}' Value='{1}'";
            Console.WriteLine(format, path, fragment.Content(path));
        }

        public static void ErrorExample(IBucket bucket, string id)
        {
            // tag::ErrorExample[]
            var builder = bucket.LookupIn<dynamic>(id).
                Get("type").
                Get("somepaththatdoesntexist").
                Get("owner");

            var fragment = builder.Execute();
            Console.WriteLine("Generic error: {0}{1}Specific Error: {2}", 
                fragment.Status, Environment.NewLine, fragment.OpStatus(1));

            Console.WriteLine("Generic error: {0}{1}Specific Error: {2}",
               fragment.Status, Environment.NewLine, fragment.OpStatus("somepaththatdoesntexist"));
            // end::ErrorExample[]
        }

        // tag::GetExample[]
        public static void GetExample(IBucket bucket, string path, string id)
        {
            var builder = bucket.LookupIn<dynamic>(id).
                Get(path).
                Execute();

            var fragment = builder.Content(path);
            Console.WriteLine(fragment);
        }
        // end::GetExample[]

        // tag::ExistsExample[]
        public static void ExistsExample(IBucket bucket, string path, string id)
        {
            var builder = bucket.LookupIn<dynamic>(id).
                Exists(path).
                Execute();

            var found = builder.Content(path);
            Console.WriteLine(found);
        }
        // end::ExistsExample[]

        // tag::InsertExample[]
        public static void InsertExample(IBucket bucket, string id, string path, string value)
        {
            var fragment = bucket.MutateIn<dynamic>(id).
                Insert(path, value, true).  // false is the default
                Execute();

            var status = fragment.OpStatus(path);
            Console.WriteLine(status);
        }
        // end::InsertExample[]

        // tag::RemoveExample[]
        public static void RemoveExample(IBucket bucket, string id, string path)
        {
            var fragment = bucket.MutateIn<dynamic>(id).
                Remove(path).
                Execute();

            var status = fragment.OpStatus(path);
            Console.WriteLine(status);
        }
        // end::RemoveExample[]

        // tag::ReplaceExample[]
        public static void ReplaceExample(IBucket bucket, string id, string path, object value)
        {
            var fragment = bucket.MutateIn<dynamic>(id).
                Replace(path, value).
                Execute();

            var status = fragment.OpStatus(path);
            Console.WriteLine(status);
        }
        // end::ReplaceExample[]

        // tag::ArrayAppendExample[]
        public static void ArrayAppendExample(IBucket bucket, string id, string path, object value)
        {
            var fragment = bucket.MutateIn<dynamic>(id).
                ArrayAppend(path, value, false).
                Execute();

            var status = fragment.OpStatus(path);
            Console.WriteLine(status);
        }
        // end::ArrayAppendExample[]

        // tag::ArrayPrependExample[]
        public static void ArrayPrependExample(IBucket bucket, string id, string path, object value)
        {
            var fragment = bucket.MutateIn<dynamic>(id).
                ArrayPrepend(path, value, false).
                Execute();

            var status = fragment.OpStatus(path);
            Console.WriteLine(status);
        }
        // end::ArrayPrependExample[]

        // tag::ArrayInsertExample[]
        public static void ArrayInsertExample(IBucket bucket, string id, string path, object value)
        {
            var fragment = bucket.MutateIn<dynamic>(id).
                ArrayInsert(path, value).
                Execute();

            var status = fragment.OpStatus(path);
            Console.WriteLine(status);
        }
        // end::ArrayInsertExample[]

        // tag::ArrayAddUniqueExample[]
        public static void ArrayAddUniqueExample(IBucket bucket, string id, string path, object value)
        {
            var fragment = bucket.MutateIn<dynamic>(id).
                ArrayAddUnique(path, value).
                Execute();

            var status = fragment.OpStatus(path);
            Console.WriteLine(status);
        }
        // end::ArrayAddUniqueExample[]

        // tag::CounterExample[]
        public static void CounterExample(IBucket bucket, string id, string path, long delta)
        {
            var fragment = bucket.MutateIn<dynamic>(id).
                Counter(path, delta).
                Execute();

            var status = fragment.OpStatus(path);
            Console.WriteLine(status);
        }
        // end::CounterExample[]
    }
}
