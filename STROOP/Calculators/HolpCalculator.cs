﻿using STROOP.Forms;
using STROOP.Managers;
using STROOP.Models;
using STROOP.Structs.Configurations;
using STROOP.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STROOP.Structs
{
    public static class HolpCalculator
    {
        private static List<(int, double, double, double)> _dataForWalking = new List<(int, double, double, double)>()
        {
            (0,-13.852560043335,82.7928466796875,43.2764892578125),
            (1,-13.8603839874268,84.1005249023438,43.2064208984375),
            (2,-13.8159141540527,84.2417602539063,43.1217041015625),
            (3,-13.8067932128906,84.3388061523438,43.032958984375),
            (4,-13.8156032562256,84.4673461914063,42.8843994140625),
            (5,-13.8143367767334,84.3573608398438,42.7357177734375),
            (6,-13.8485641479492,84.4801635742188,42.5528564453125),
            (7,-13.913293838501,84.3718872070313,42.3017578125),
            (8,-13.9324378967285,84.2554931640625,42.12255859375),
            (9,-14.0344524383545,83.8282470703125,41.887451171875),
            (10,-14.1320991516113,83.65185546875,41.6539306640625),
            (11,-14.2047386169434,83.2032470703125,41.481689453125),
            (12,-14.3437423706055,82.9824829101563,41.230224609375),
            (13,-14.4278945922852,82.4707641601563,41.1116943359375),
            (14,-14.6146621704102,81.8866577148438,40.9158935546875),
            (15,-14.7635688781738,81.2904663085938,40.7923583984375),
            (16,-14.983922958374,80.6292724609375,40.6275634765625),
            (17,-15.2353763580322,79.8695678710938,40.525634765625),
            (18,-15.4751510620117,79.0504760742188,40.5074462890625),
            (19,-15.7339839935303,78.1624145507813,40.528564453125),
            (20,-16.0744380950928,77.209716796875,40.4693603515625),
            (21,-16.350076675415,76.2724609375,40.4874267578125),
            (22,-16.6590099334717,75.2498779296875,40.524169921875),
            (23,-16.9654483795166,74.2208862304688,40.5496826171875),
            (24,-17.286901473999,73.1162719726563,40.60302734375),
            (25,-17.5732021331787,72.0418090820313,40.662109375),
            (26,-17.8684062957764,70.9493408203125,40.6968994140625),
            (27,-18.1436023712158,70.1430053710938,40.7091064453125),
            (28,-18.4258403778076,69.06982421875,40.7000732421875),
            (29,-18.6577587127686,68.073974609375,40.6951904296875),
            (30,-18.8827457427979,66.8560180664063,40.662353515625),
            (31,-19.0599193572998,65.9683227539063,40.640380859375),
            (32,-19.2313365936279,65.8605346679688,40.594970703125),
            (33,-19.3553791046143,66.3665161132813,40.5447998046875),
            (34,-19.4837818145752,67.1422729492188,40.4661865234375),
            (35,-19.5892887115479,67.9154052734375,40.43017578125),
            (36,-19.659029006958,69.2606811523438,40.4078369140625),
            (37,-19.7250804901123,70.356201171875,40.3907470703125),
            (38,-19.7577571868896,71.4926147460938,40.40869140625),
            (39,-19.7571697235107,72.4199829101563,40.462158203125),
            (40,-19.7728824615479,73.1315307617188,40.45849609375),
            (41,-19.7376079559326,73.8190307617188,40.5723876953125),
            (42,-19.7122821807861,74.5830078125,40.6121826171875),
            (43,-19.6650981903076,75.282958984375,40.739013671875),
            (44,-19.57346534729,75.8268432617188,40.88525390625),
            (45,-19.4797077178955,76.1210327148438,41.033203125),
            (46,-19.3439426422119,76.4605102539063,41.2265625),
            (47,-19.2333011627197,76.2872314453125,41.37890625),
            (48,-19.0889110565186,76.1022338867188,41.597900390625),
            (49,-18.9064617156982,75.9515380859375,41.857421875),
            (50,-18.7486705780029,75.5679321289063,42.0560302734375),
            (51,-18.5583438873291,75.1459350585938,42.33203125),
            (52,-18.3875865936279,74.7825317382813,42.52978515625),
            (53,-18.1501483917236,74.1698608398438,42.83203125),
            (54,-17.9086894989014,73.806396484375,43.1324462890625),
            (55,-17.7250003814697,73.1712646484375,43.34814453125),
            (56,-17.4643001556396,72.8579711914063,43.6300048828125),
            (57,-17.1985988616943,72.2935791015625,43.9110107421875),
            (58,-16.9619617462158,71.688720703125,44.1668701171875),
            (59,-16.7159366607666,71.3931274414063,44.3900146484375),
            (60,-16.4704761505127,70.817626953125,44.6248779296875),
            (61,-16.1780300140381,70.2903442382813,44.8857421875),
            (62,-15.9158897399902,70.0347900390625,45.0811767578125),
            (63,-15.6951370239258,69.4375,45.2755126953125),
            (64,-15.4251079559326,69.2108764648438,45.447509765625),
            (65,-15.1985931396484,68.4242553710938,45.596435546875),
            (66,-14.9584121704102,67.7183227539063,45.701416015625),
            (67,-14.7734146118164,67.4125366210938,45.7999267578125),
            (68,-14.5395526885986,68.2246704101563,45.871826171875),
            (69,-14.385705947876,70.1324462890625,45.7674560546875),
            (70,-14.2014904022217,72.6931762695313,45.574951171875),
            (71,-14.1282138824463,75.119140625,45.1312255859375),
            (72,-13.9819869995117,77.0831298828125,44.716552734375),
            (73,-13.9631118774414,78.2352905273438,44.156494140625),
            (74,-13.8647117614746,79.316162109375,43.736083984375),
            (75,-13.8919486999512,80.1707763671875,43.3465576171875),
        };

        private static List<(int, double, double, double)> _dataForStanding = new List<(int, double, double, double)>()
        {
            (0,-14.9768142700195,70.0527954101563,46.1116943359375),
            (1,-15.0781002044678,68.8441772460938,46.2886962890625),
            (2,-15.1815433502197,67.6535034179688,46.4891357421875),
            (3,-15.2864570617676,66.4968872070313,46.6727294921875),
            (4,-15.4240989685059,65.3958129882813,46.7882080078125),
            (5,-15.5248756408691,64.2696533203125,46.9483642578125),
            (6,-15.6575241088867,63.5349731445313,47.0325927734375),
            (7,-15.7512474060059,62.8151245117188,47.1492919921875),
            (8,-15.8002777099609,62.2562866210938,47.2369384765625),
            (9,-15.8486957550049,62.0972290039063,47.2794189453125),
            (10,-15.8909912109375,62.0515747070313,47.27490234375),
            (11,-15.8591861724854,62.265380859375,47.27587890625),
            (12,-15.859338760376,62.816650390625,47.187744140625),
            (13,-15.7759399414063,63.7854614257813,47.0892333984375),
            (14,-15.6890602111816,64.8377685546875,46.96875),
            (15,-15.5938949584961,65.9906616210938,46.784423828125),
            (16,-15.4942493438721,67.1390991210938,46.58642578125),
            (17,-15.3519325256348,67.94384765625,46.4541015625),
            (18,-15.2373886108398,69.06005859375,46.2559814453125),
            (19,-15.1235389709473,69.7003173828125,46.1351318359375),
            (20,-14.9768142700195,70.0527954101563,46.1116943359375),
            (21,-14.8577976226807,70.2546997070313,46.1241455078125),
            (22,-14.7283592224121,70.18115234375,46.167236328125),
            (23,-14.5577449798584,69.81005859375,46.2996826171875),
            (24,-14.3835906982422,69.622314453125,46.4842529296875),
            (25,-14.2441596984863,69.1331787109375,46.658935546875),
            (26,-14.0631008148193,68.68505859375,46.8543701171875),
            (27,-13.9271926879883,68.18115234375,47.030517578125),
            (28,-13.8096122741699,67.6304931640625,47.2154541015625),
            (29,-13.7399406433105,67.3751220703125,47.305908203125),
            (30,-13.7294921875,67.3475952148438,47.339111328125),
            (31,-13.7399406433105,67.3751220703125,47.305908203125),
            (32,-13.8096122741699,67.6304931640625,47.2154541015625),
            (33,-13.9271926879883,68.18115234375,47.030517578125),
            (34,-14.0631008148193,68.68505859375,46.8543701171875),
            (35,-14.2348442077637,69.1707763671875,46.6429443359375),
            (36,-14.3747138977051,69.6599731445313,46.4674072265625),
            (37,-14.5577449798584,69.81005859375,46.2996826171875),
            (38,-14.7283592224121,70.18115234375,46.167236328125),
            (39,-14.8577976226807,70.2546997070313,46.1241455078125),
            (40,-14.9768142700195,70.0527954101563,46.1116943359375),
            (41,-15.1239433288574,69.745849609375,46.1343994140625),
            (42,-15.2446212768555,69.0670776367188,46.2738037109375),
            (43,-15.3988609313965,68.053955078125,46.4130859375),
            (44,-15.5546684265137,67.1847534179688,46.6026611328125),
            (45,-15.6641502380371,66.0863647460938,46.8216552734375),
            (46,-15.8019599914551,65.0361328125,46.9552001953125),
            (47,-15.900447845459,64.0007934570313,47.117431640625),
            (48,-15.9886722564697,63.0756225585938,47.221923828125),
            (49,-16.0358943939209,62.5552368164063,47.295654296875),
            (50,-16.0395030975342,62.44921875,47.343505859375),
            (51,-16.0358943939209,62.5552368164063,47.295654296875),
            (52,-15.9487285614014,63.134521484375,47.245361328125),
            (53,-15.8516540527344,64.1402587890625,47.10791015625),
            (54,-15.7057952880859,65.27294921875,46.9495849609375),
            (55,-15.5549392700195,66.400390625,46.7799072265625),
            (56,-15.3991146087646,67.5536499023438,46.5782470703125),
            (57,-15.238431930542,68.4193725585938,46.3848876953125),
            (58,-15.1240825653076,69.3726806640625,46.22314453125),
            (59,-15.0214500427246,69.9351196289063,46.13720703125),
            (60,-14.9768142700195,70.0527954101563,46.1116943359375),
        };

        private static Dictionary<int, (double, double, double)> _dictionaryForWalking;
        private static Dictionary<int, (double, double, double)> _dictionaryForStanding;
        public static readonly int WALKING_COUNT = 76;
        public static readonly int STANDING_COUNT = 61;

        static HolpCalculator()
        {
            _dictionaryForWalking = new Dictionary<int, (double, double, double)>();
            foreach ((int index, double x, double y, double z) in _dataForWalking)
            {
                _dictionaryForWalking[index] = (x, y, z);
            }

            _dictionaryForStanding = new Dictionary<int, (double, double, double)>();
            foreach ((int index, double x, double y, double z) in _dataForStanding)
            {
                _dictionaryForStanding[index] = (x, y, z);
            }
        }

        public static (float x, float y, float z) GetHolpForWalking(int index)
        {
            if (!_dictionaryForWalking.ContainsKey(index)) return (float.NaN, float.NaN, float.NaN);
            (double xOffset, double yOffset, double zOffset) = _dictionaryForWalking[index];
            return ((float)xOffset, (float)yOffset, (float)zOffset);
        }

        public static (float x, float y, float z) GetHolpForStanding(int index)
        {
            if (!_dictionaryForStanding.ContainsKey(index)) return (float.NaN, float.NaN, float.NaN);
            (double xOffset, double yOffset, double zOffset) = _dictionaryForStanding[index];
            return ((float)xOffset, (float)yOffset, (float)zOffset);
        }

        public static (float x, float y, float z) GetHolpForWalking(
            int index, float marioX, float marioY, float marioZ, ushort marioAngle)
        {
            if (!_dictionaryForWalking.ContainsKey(index)) return (float.NaN, float.NaN, float.NaN);
            (double xOffset, double yOffset, double zOffset) = _dictionaryForWalking[index];

            double vectorMagnitude = MoreMath.GetHypotenuse(xOffset, zOffset);
            double vectorAngle = MoreMath.AngleTo_AngleUnits(xOffset, zOffset);
            double rotatedAngle = vectorAngle + MoreMath.NormalizeAngleTruncated(marioAngle);
            (double rotatedX, double rotatedZ) = MoreMath.GetComponentsFromVector(vectorMagnitude, rotatedAngle);

            double offsetX = rotatedX + marioX;
            double offsetY = yOffset + marioY;
            double offsetZ = rotatedZ + marioZ;

            return ((float)offsetX, (float)offsetY, (float)offsetZ);
        }

        public static (float x, float y, float z) GetHolpForStanding(
            int index, float marioX, float marioY, float marioZ, ushort marioAngle)
        {
            if (!_dictionaryForStanding.ContainsKey(index)) return (float.NaN, float.NaN, float.NaN);
            (double xOffset, double yOffset, double zOffset) = _dictionaryForStanding[index];

            double vectorMagnitude = MoreMath.GetHypotenuse(xOffset, zOffset);
            double vectorAngle = MoreMath.AngleTo_AngleUnits(xOffset, zOffset);
            double rotatedAngle = vectorAngle + MoreMath.NormalizeAngleTruncated(marioAngle);
            (double rotatedX, double rotatedZ) = MoreMath.GetComponentsFromVector(vectorMagnitude, rotatedAngle);

            double offsetX = rotatedX + marioX;
            double offsetY = yOffset + marioY;
            double offsetZ = rotatedZ + marioZ;

            return ((float)offsetX, (float)offsetY, (float)offsetZ);
        }
    }
}
