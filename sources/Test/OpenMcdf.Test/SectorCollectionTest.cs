﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace OpenMcdf.Test
{
    /// <summary>
    /// This is a test class for SectorCollectionTest and is intended
    /// to contain all SectorCollectionTest Unit Tests
    /// </summary>
    [TestClass()]
    public class SectorCollectionTest
    {
        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        #region Additional test attributes
        //
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion

        /// <summary>
        /// A test for Count
        /// </summary>
        [TestMethod()]
        public void CountTest()
        {
            SectorCollection target = new SectorCollection(); // TODO: Initialize to an appropriate value

            Assert.AreEqual(0, target.Count);
            Sector s = new Sector(4096);

            target.Add(s);
            Assert.AreEqual(1, target.Count);

            for (int i = 0; i < 5000; i++)
                target.Add(s);

            Assert.AreEqual(5001, target.Count);
        }

        /// <summary>
        /// A test for Item
        /// </summary>
        [TestMethod()]
        public void ItemTest()
        {
            int count = 37;

            SectorCollection target = new SectorCollection();
            int index = 0;

            Sector expected = new Sector(4096);
            target.Add(null);

            Sector actual;
            target[index] = expected;
            actual = target[index];

            Assert.AreEqual(expected, actual);
            Assert.AreEqual(expected.Id, actual.Id);
            Assert.ThrowsException<CFException>(() => target[count + 100]);
            Assert.ThrowsException<CFException>(() => target[-1]);
        }

        /// <summary>
        /// A test for SectorCollection Constructor
        /// </summary>
        [TestMethod()]
        public void SectorCollectionConstructorTest()
        {
            SectorCollection target = new SectorCollection();

            Assert.IsNotNull(target);
            Assert.AreEqual(0, target.Count);

            Sector s = new Sector(4096);
            target.Add(s);
            Assert.AreEqual(1, target.Count);
        }

        /// <summary>
        /// A test for Add
        /// </summary>
        [TestMethod()]
        public void AddTest()
        {
            SectorCollection target = new SectorCollection();
            for (int i = 0; i < 579; i++)
            {
                target.Add(null);
            }

            Sector item = new Sector(4096);
            target.Add(item);
            Assert.AreEqual(580, target.Count);
        }

        /// <summary>
        /// A test for GetEnumerator
        /// </summary>
        [TestMethod()]
        public void GetEnumeratorTest()
        {
            SectorCollection target = new SectorCollection();
            for (int i = 0; i < 579; i++)
            {
                target.Add(null);
            }

            Sector item = new Sector(4096);
            target.Add(item);
            Assert.AreEqual(580, target.Count);

            int count = 0;
            using IEnumerator<Sector> enumerator = target.GetEnumerator();
            while (enumerator.MoveNext())
            {
                count++;
            }
            Assert.AreEqual(580, count);
        }
    }
}
